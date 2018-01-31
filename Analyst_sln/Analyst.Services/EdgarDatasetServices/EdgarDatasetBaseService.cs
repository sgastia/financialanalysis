﻿using Analyst.DBAccess.Contexts;
using Analyst.Domain.Edgar;
using Analyst.Domain.Edgar.Datasets;
using Analyst.Domain.Edgar.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Analyst.Services.EdgarDatasetServices
{


    public interface IEdgarDatasetBaseService<T>: IDisposable where T :class, IEdgarDatasetFile
    {
        ConcurrentDictionary<string, int> GetAsConcurrent(int datasetId);
        void Process(EdgarTaskState state,bool processBulk, bool processInParallel, string fileToProcess, string fieldToUpdate);
       
    }

    public abstract class EdgarDatasetBaseService<T>: IEdgarDatasetBaseService<T> where T:class,IEdgarDatasetFile
    {
        private const int DEFAULT_MAX_ERRORS_ALLOWED = int.MaxValue;
        protected abstract log4net.ILog Log { get; }

        protected int MaxErrorsAllowed
        {
            get
            {
                string strValue = ConfigurationManager.AppSettings["maxerrorsallowed"];
                int iValue;
                if (int.TryParse(strValue, out iValue))
                    return iValue;
                else
                    return DEFAULT_MAX_ERRORS_ALLOWED;
            }
        }

        public ConcurrentDictionary<string, int> GetAsConcurrent(int datasetId)
        {
            ConcurrentDictionary<string, int> ret = new ConcurrentDictionary<string, int>();
            using (IAnalystRepository repository = new AnalystRepository(new AnalystContext()))
            {
                IList<EdgarTuple> keysId = GetKeys(repository, datasetId);
                foreach (EdgarTuple t in keysId)
                {
                    ret.TryAdd(t.Key, t.Id);
                }
            }
            return ret;
        }

 

        public virtual void Process(EdgarTaskState state, bool processBulk, bool processInParallel, string fileToProcess,string fieldToUpdate)
        {
            try
            {
                Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
                Log.Info("Datasetid " + state.Dataset.Id.ToString() + " -- " + fileToProcess + " -- BEGIN process");
                int processedLines;
                if (!IsAlreadyProcessed(state.Dataset, fieldToUpdate,out processedLines))
                {
                    string cacheFolder = ConfigurationManager.AppSettings["cache_folder"];
                    string tsvFileName = state.Dataset.RelativePath.Replace("/", "\\").Replace(".zip", "") + "\\" + fileToProcess;
                    string filepath = cacheFolder + tsvFileName;
                    string[] allLines = File.ReadAllLines(filepath);
                    string header = allLines[0];

                    UpdateTotalField(state,fieldToUpdate, allLines.Length - 1);

                    if (processBulk)
                        ProcessBulk(fileToProcess,fieldToUpdate, state, allLines, header);
                    else
                        ProcessLineByLine(processedLines,fileToProcess, fieldToUpdate, state, allLines, header,cacheFolder,tsvFileName, processInParallel);
                }
                else
                {
                    state.FileNameToReprocess = null;
                    Log.Info("Datasetid " + state.Dataset.Id.ToString() + " -- " + fileToProcess + " -- The complete file is already processed");
                }
                
                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                Log.Info("Datasetid " + state.Dataset.Id.ToString() + " -- " + fileToProcess + " -- time: " + elapsedTime);
                state.ResultOk = true;
            }
            catch (Exception ex)
            {
                state.ResultOk = false;
                state.Exception = new EdgarDatasetException(fileToProcess,ex);
                Log.Fatal("Datasetid " + state.Dataset.Id.ToString() + " -- " + fileToProcess + " -- Error: " + ex.Message, ex);
            }
        }

        public void ProcessLineByLine(int processedLines, string fileToProcess, string fieldToUpdate, EdgarTaskState state, string[] allLines,string header, string cacheFolder, string tsvFileName,bool processInParallel)
        {
            ConcurrentBag<int> missing;
            if (processedLines == 0)
            {
                missing = null;
            }
            else
            {
                missing = GetMissingLines(state.Dataset.Id, allLines.Length - 1);
            }

            ConcurrentDictionary<int, string> failedLines = new ConcurrentDictionary<int, string>();
            if (processInParallel && allLines.Length > 1)
            {
                //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/custom-partitioners-for-plinq-and-tpl?view=netframework-4.5.2

                // Partition the entire source array.
                OrderablePartitioner<Tuple<int, int>> rangePartitioner = Partitioner.Create(1, allLines.Length);

                // Loop over the partitions in parallel.
                Parallel.ForEach(rangePartitioner, (range, loopState) =>
                {
                    ProcessRange(fileToProcess, state, range, allLines, header, missing, failedLines);
                });

            }
            else
            {
                ProcessRange(fileToProcess, state, new Tuple<int, int>(1, allLines.Length), allLines, header, missing, failedLines);
            }
            state.FileNameToReprocess = WriteFailedLines(cacheFolder, tsvFileName, header, failedLines, allLines.Length);

        }

        private void ProcessBulk(string fileToProcess,string fieldToUpdate, EdgarTaskState state, string[] allLines, string header)
        {
            //https://msdn.microsoft.com/en-us/library/ex21zs8x(v=vs.110).aspx
            //https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/transaction-and-bulk-copy-operations
            using (SQLAnalystRepository repo = new SQLAnalystRepository(new AnalystContext()))
            {
                DataTable dt = GetEmptyDataTable(repo);
                List<string> fieldNames = header.Split('\t').ToList();
                //first line is the header
                for (int i=1;i<allLines.Length;i++)
                {
                    string line = allLines[i];
                    if (!string.IsNullOrEmpty(line))
                    {
                        List<string> fields = line.Split('\t').ToList();
                        DataRow dr = dt.NewRow();
                        Parse(fieldNames,fields, i + 1, dr,state.Dataset.Id);
                        dt.Rows.Add(dr);
                    }
                }
                BulkCopy(repo, dt);
                UpdateProcessed(state, fieldToUpdate, dt.Rows.Count);
            }
        }

        private void UpdateProcessed(EdgarTaskState state, string fieldToUpdate, int value)
        {
            UpdateField("Processed", fieldToUpdate, state, value);
        }

        private void UpdateTotalField(EdgarTaskState state, string fieldToUpdate, int value)
        {
            UpdateField("Total", fieldToUpdate, state, value);
        }
        private void UpdateField(string prefix, string fieldToUpdate, EdgarTaskState state, int value)
        {
            string field = prefix + fieldToUpdate;
            int currentValue = (int)state.Dataset.GetType().GetProperty(field).GetValue(state.Dataset);
            if (currentValue == 0)
            {
                state.Dataset.GetType().GetProperty(field).SetValue(state.Dataset, value);
                state.DatasetSharedRepo.UpdateEdgarDataset(state.Dataset, field);
            }
        }


        private string WriteFailedLines(string folder,string fileName, string header, ConcurrentDictionary<int,string> failedLines,int totalLines)
        {
            string newFileName = null;
            if(failedLines.Count > 0)
            {
                newFileName = fileName + "_failed_" + DateTime.Now.ToString("yyyyMMddmmss") + ".tsv";
                StreamWriter sw = File.CreateText(folder + newFileName);
                sw.WriteLine(header);
                for(int i=1;i<totalLines;i++)
                {
                    if (failedLines.ContainsKey(i + 1))
                    {
                        sw.WriteLine(failedLines[i + 1]);
                    }
                    else
                        sw.WriteLine("");
                }
                sw.Close();
            }
            return newFileName;
        }

        private bool IsAlreadyProcessed(EdgarDataset ds, string fieldToUpdate, out int processed)
        {
            using (IAnalystRepository repo = new AnalystRepository(new AnalystContext()))
            {
                int savedInDb = repo.GetCount<T>();
                processed = (int)ds.GetType().GetProperty("Processed" + fieldToUpdate).GetValue(ds);
                int total = (int)ds.GetType().GetProperty("Total" + fieldToUpdate).GetValue(ds);
                return savedInDb == processed && processed == total && total != 0;
            }
        }

        protected void ProcessRange(string fileName,EdgarTaskState state, Tuple<int, int> range, string[] allLines, string header,ConcurrentBag<int> missing, ConcurrentDictionary<int,string> failedLines)
        {
            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            string rangeMsg = "Datasetid " + state.Dataset.Id.ToString() + " -- " + fileName + " -- range: " + range.Item1 + " to " + range.Item2;
            Log.Info(rangeMsg + " -- BEGIN");

            /*
            EF isn't thread safe and it doesn't allow parallel
            https://stackoverflow.com/questions/12827599/parallel-doesnt-work-with-entity-framework
            https://stackoverflow.com/questions/9099359/entity-framework-and-multi-threading
            https://social.msdn.microsoft.com/Forums/en-US/e5cb847c-1d77-4cd0-abb7-b61890d99fae/multithreading-and-the-entity-framework?forum=adodotnetentityframework
            solution: only 1 context for the entiry partition --> works
            */
            using (IAnalystRepository repo = new AnalystRepository(new AnalystContext()))
            {
                //It improves performance
                //https://msdn.microsoft.com/en-us/library/jj556205(v=vs.113).aspx
                repo.ContextConfigurationAutoDetectChangesEnabled = false;
                try
                {
                    List<string> fieldNames = header.Split('\t').ToList();
                    List<Exception> exceptions = new List<Exception>();
                    string line = null;
                    
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        try
                        {
                            if (missing == null || missing.Contains(i+1))
                            {
                                Log.Debug(rangeMsg + " -- parsing[" + i.ToString() + "]: " + line);
                                line = allLines[i];
                                if (!string.IsNullOrEmpty(line))
                                {
                                    List<string> fields = line.Split('\t').ToList();
                                    T file = Parse(repo, fieldNames, fields, i + 1);//i+1: indexes starts with 0 but header is line 1 and the first row is line 2
                                    Add(repo, state.Dataset, file);
                                    int result;
                                    missing.TryTake(out result);
                                }
                            }
                            else
                            {
                                if(missing.IsEmpty)
                                {
                                    Log.Info(rangeMsg + " -- missing collection is empty");
                                    break;
                                }
                            }

                        }
                        catch(Exception ex)
                        {
                            EdgarLineException elex = new EdgarLineException(fileName, i, ex);
                            exceptions.Add(elex);
                            failedLines.TryAdd(i,line);
                            Log.Error(rangeMsg + " -- line[" + i.ToString() + "]: " + line);
                            Log.Error(rangeMsg + " -- line[" + i.ToString() + "]: " + ex.Message, elex);
                            if (exceptions.Count > MaxErrorsAllowed)
                            {
                                Log.Fatal(rangeMsg + " -- line[" + i.ToString() + "]: max errors allowed reached", ex);
                                throw new EdgarDatasetException(fileName, exceptions);
                            }
                            
                        }
                    }
                    

                }
                finally
                {
                    repo.ContextConfigurationAutoDetectChangesEnabled = true;
                }
            }
            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Log.Info(rangeMsg + " -- END");
            Log.Info(rangeMsg + " -- time: " + elapsedTime);

        }

        public abstract void Add(IAnalystRepository repo, EdgarDataset dataset, T file);

        public abstract T Parse(IAnalystRepository repository, List<string> fieldNames, List<string> fields, int lineNumber);
     
        public abstract IList<EdgarTuple> GetKeys(IAnalystRepository repository, int datasetId);

        public abstract string GetKey(List<string> fieldNames, List<string> fields);

        public abstract void BulkCopy(SQLAnalystRepository repo, DataTable dt);

        public abstract DataTable GetEmptyDataTable(SQLAnalystRepository repo);

        public abstract void Parse(List<string> fieldNames, List<string> fields, int lineNumber, DataRow dr, int edgarDatasetId);

        public abstract ConcurrentBag<int> GetMissingLines(int datasetId, int totalLines);

        public void Dispose()
        {

        }
    }
}