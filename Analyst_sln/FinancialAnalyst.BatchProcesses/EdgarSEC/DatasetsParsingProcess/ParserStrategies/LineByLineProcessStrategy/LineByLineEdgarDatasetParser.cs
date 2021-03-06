﻿using FinancialAnalyst.BatchProcesses.EdgarSEC.DatasetsParsingProcess;
using FinancialAnalyst.BatchProcesses.EdgarSEC.DatasetsParsingProcess.ParserStrategies.Interfaces;
using FinancialAnalyst.Common.Entities.EdgarSEC.Datasets;
using FinancialAnalyst.Common.Entities.EdgarSEC.Repositories;
using FinancialAnalyst.Common.Exceptions.EdgarSEC;
using FinancialAnalyst.DataAccess.EdgarSEC.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialAnalyst.BatchProcesses.EdgarSEC.DatasetsParsingProcess.ParserStrategies.LineByLineProcessStrategy.ConcreteParsers
{
    public abstract class LineByLineEdgarDatasetParser<T>: TemplateMethodOfEdgarDatasetParser<T>, IEdgarDatasetParser<T> where T : class, IEdgarDatasetFile
    {
        public override void ProcessFile(ConcurrentBag<int> missing, string fileToProcess, string fieldToUpdate, EdgarTaskState state, string[] allLines, string header, string cacheFolder, string tsvFileName, bool processInParallel)
        {

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

        protected void ProcessRange(string fileToProcess, EdgarTaskState state, Tuple<int, int> range, string[] allLines, string header,ConcurrentBag<int> missing, ConcurrentDictionary<int,string> failedLines)
        {
            Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            string rangeMsg = "Datasetid " + state.Dataset.Id.ToString() + " -- " + fileToProcess + " -- range: " + range.Item1 + " to " + range.Item2;
            Log.Info(rangeMsg + " -- BEGIN");

            /*
            EF isn't thread safe and it doesn't allow parallel
            https://stackoverflow.com/questions/12827599/parallel-doesnt-work-with-entity-framework
            https://stackoverflow.com/questions/9099359/entity-framework-and-multi-threading
            https://social.msdn.microsoft.com/Forums/en-US/e5cb847c-1d77-4cd0-abb7-b61890d99fae/multithreading-and-the-entity-framework?forum=adodotnetentityframework
            solution: only 1 context for the entiry partition --> works
            */
            using (IEdgarDatasetsRepository repo = new EdgarRepository())
            {
                //It improves performance
                //https://msdn.microsoft.com/en-us/library/jj556205(v=vs.113).aspx
                repo.ContextConfigurationAutoDetectChangesEnabled = false;
                try
                {
                    List<string> fieldNames = header.Split('\t').ToList();
                    List<Exception> exceptions = new List<Exception>();
                    string line = null;
                    int lineNumber = 0;
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        try
                        {
                            lineNumber = i + 1;//i+1: indexes starts with 0 but header is line 1 and the first row is line 2
                            //It will be processed if:
                            //it's the first time (missing == null) 
                            //or it's processed again and line wasn't processed the firs time (missing.Contains(i+1))
                            if (missing == null || missing.Contains(i+1))
                            {
                                Log.Debug(rangeMsg + " -- parsing[" + i.ToString() + "]: " + line);
                                line = allLines[i];
                                if (!string.IsNullOrEmpty(line))//files with error lines has an empty line for processed lines
                                {

                                    List<string> fields = line.Split('\t').ToList();
                                    T file = Parse(repo, fieldNames, fields, lineNumber);
                                    Add(repo, state.Dataset, file);
                                }
                            }

                        }
                        catch(Exception ex)
                        {
                            EdgarLineException elex = new EdgarLineException(fileToProcess, lineNumber, ex);
                            exceptions.Add(elex);
                            failedLines.TryAdd(lineNumber, line);
                            Log.Error(rangeMsg + " -- line[" + lineNumber.ToString() + "]: " + line);
                            Log.Error(rangeMsg + " -- line[" + lineNumber.ToString() + "]: " + ex.Message, elex);
                            if (exceptions.Count > MaxErrorsAllowed)
                            {
                                Log.Fatal(rangeMsg + " -- line[" + i.ToString() + "]: max errors allowed reached", ex);
                                throw new EdgarDatasetException(fileToProcess, exceptions);
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

        public abstract void Add(IEdgarDatasetsRepository repo, EdgarDataset dataset, T file);

        public abstract T Parse(IEdgarDatasetsRepository repository, List<string> fieldNames, List<string> fields, int lineNumber);

    }
}