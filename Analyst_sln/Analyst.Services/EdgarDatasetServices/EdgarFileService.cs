﻿using Analyst.DBAccess.Contexts;
using Analyst.Domain.Edgar.Datasets;
using Analyst.Domain.Edgar.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Analyst.Services.EdgarDatasetServices
{
    public interface IEdgarFileService<T>: IDisposable where T :class, IEdgarDatasetFile
    {
        ConcurrentDictionary<string, T> GetAsConcurrent(int datasetId);
        ConcurrentDictionary<string, T> GetAsConcurrent(int datasetId,string[] includes);
        void Process(EdgarTaskState state, bool processInParallel, string fileToProcess, string fieldToUpdate);
    }

    public abstract class EdgarFileService<T>:IEdgarFileService<T> where T:class,IEdgarDatasetFile
    {
        private const int DEFAULT_MAX_ERRORS_ALLOWED = 10;
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

        public ConcurrentDictionary<string, T> GetAsConcurrent(int datasetId)
        {
            return GetAsConcurrent(datasetId, null);
        }

        public ConcurrentDictionary<string, T> GetAsConcurrent(int datasetId, string[] includes)
        {
            ConcurrentDictionary<string, T> ret = new ConcurrentDictionary<string, T>();
            IAnalystRepository repository = new AnalystRepository(new AnalystContext());

            IList<T> xs = repository.GetByDatasetId<T>(datasetId,includes);
            foreach (T x in xs)
            {
                ret.TryAdd(x.Key, x);
            }
            return ret;
        }

        public void Process(EdgarTaskState state,bool processInParallel, string fileToProcess,string fieldToUpdate)
        {
            try
            {
                Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
                Log.Info("Begin process " + fileToProcess);
                if (!IsAlreadyProcessed(state.Dataset, fieldToUpdate))
                {
                    string cacheFolder = ConfigurationManager.AppSettings["cache_folder"];
                    string filepath = cacheFolder + state.Dataset.RelativePath.Replace("/", "\\").Replace(".zip", "") + "\\" + fileToProcess;
                    string[] allLines = File.ReadAllLines(filepath);
                    string header = allLines[0];
                    string field = "Total" + fieldToUpdate;
                    state.Dataset.GetType().GetProperty(field).SetValue(state.Dataset, allLines.Length - 1);
                    state.DatasetSharedRepo.UpdateEdgarDataset(state.Dataset, field);
                    ConcurrentDictionary<string, T> existing = this.GetAsConcurrent(state.Dataset.Id);//go to DB once and check item per item to exists to avoid duplicates, process can be stopped and resumed
                    if (processInParallel && allLines.Length > 1)
                    {
                        //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/custom-partitioners-for-plinq-and-tpl?view=netframework-4.5.2

                        // Partition the entire source array.
                        OrderablePartitioner<Tuple<int, int>> rangePartitioner = Partitioner.Create(1, allLines.Length);

                        // Loop over the partitions in parallel.
                        Parallel.ForEach(rangePartitioner, (range, loopState) =>
                        {
                            ProcessRange(fileToProcess, state, range, allLines, header, existing);
                        });
                    }
                    else
                    {
                        ProcessRange(fileToProcess, state, new Tuple<int, int>(1, allLines.Length), allLines, header, existing);
                    }
                }
                else
                {
                    Log.Info("The complete file " + fileToProcess + " has been processed");
                }
                watch.Stop();
                Log.Info("End process " + fileToProcess + " - time: " + watch.Elapsed.ToString());
                state.ResultOk = true;
            }
            catch (Exception ex)
            {
                state.ResultOk = false;
                state.Exception = new EdgarDatasetException(fileToProcess,ex);
            }
        }

        private bool IsAlreadyProcessed(EdgarDataset ds, string fieldToUpdate)
        {
            using (IAnalystRepository repo = new AnalystRepository(new AnalystContext()))
            {
                int savedInDb = repo.GetCount<T>();
                int processed = (int)ds.GetType().GetProperty("Processed" + fieldToUpdate).GetValue(ds);
                int total = (int)ds.GetType().GetProperty("Total" + fieldToUpdate).GetValue(ds);
                return savedInDb == processed && processed == total && total != 0;
            }
        }

        protected void ProcessRange(string fileName,EdgarTaskState state, Tuple<int, int> range, string[] allLines, string header, ConcurrentDictionary<string, T> existing)
        {
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
                            Log.Debug(fileName + ": parsing line: " + i.ToString());
                            line = allLines[i];
                            List<string> fields = line.Split('\t').ToList();

                            T file = Parse(repo, fieldNames, fields, i+1,existing);//i+1: indexes starts with 0 but header is line 1 and the first row is line 2
                            Add(repo, state.Dataset, file);
                        }
                        catch(Exception ex)
                        {
                            EdgarLineException elex = new EdgarLineException(fileName, i, ex);
                            exceptions.Add(elex);
                            Log.Error(fileName + "[" + i.ToString() + "]: " + line);
                            Log.Error(fileName + "["+ i.ToString() + "]: " + ex.Message, elex);
                            if (exceptions.Count > MaxErrorsAllowed)
                            {
                                Log.Fatal(fileName + ": max errors allowed reached", ex);
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
        }

        public abstract void Add(IAnalystRepository repo, EdgarDataset dataset, T file);
        public abstract T Parse(IAnalystRepository repository, List<string> fieldNames, List<string> fields, int lineNumber, ConcurrentDictionary<string, T> existing);
     
        public void Dispose()
        {

        }
    }
}