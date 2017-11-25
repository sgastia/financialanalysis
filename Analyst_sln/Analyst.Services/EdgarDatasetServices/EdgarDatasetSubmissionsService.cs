﻿using Analyst.DBAccess.Contexts;
using Analyst.Domain.Edgar;
using Analyst.Domain.Edgar.Datasets;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using log4net;
using System.Globalization;

namespace Analyst.Services.EdgarDatasetServices
{
    public interface IEdgarDatasetSubmissionsService : IEdgarFileService<EdgarDatasetSubmission>
    {

    }
    public class EdgarDatasetSubmissionsService: EdgarFileService<EdgarDatasetSubmission>, IEdgarDatasetSubmissionsService
    {
        private readonly ILog log;
        protected override ILog Log
        {
            get
            {
                return log;
            }
        }
        public EdgarDatasetSubmissionsService()
        {
            log = log4net.LogManager.GetLogger(this.GetType().Name);
        }
        public override void Add(IAnalystRepository repo, EdgarDataset dataset, EdgarDatasetSubmission file)
        {
            repo.Add(dataset, file);
        }

        public override EdgarDatasetSubmission Parse(IAnalystRepository repository, List<string> fieldNames, List<string> fields, int lineNumber, ConcurrentDictionary<string, int> existing)
        {
            //Example
            //adsh	cik	name	sic	countryba	stprba	cityba	zipba	bas1	bas2	baph	countryma	stprma	cityma	zipma	mas1	mas2	countryinc	stprinc	ein	former	changed	afs	wksi	fye	form	period	fy	fp	filed	accepted	prevrpt	detail	instance	nciks	aciks	pubfloatusd	floatdate	floataxis	floatmems
            //0000002178 - 16 - 000103    2178    ADAMS RESOURCES &ENERGY, INC.  5172    US TX  HOUSTON 77027   17 S.BRIAR HOLLOW LN.      7138813600  US TX  HOUSTON 77001   P O BOX 844     US DE  741753147   ADAMS RESOURCES &ENERGY INC    19920703    2 - ACC   0   1231    10 - Q    20160930    2016    Q3  20161109    2016 - 11 - 09 12:49:00.0   0   1   ae - 20160930.xml 1

            EdgarDatasetSubmission sub;
            string adsh = fields[fieldNames.IndexOf("adsh")];
            if (existing.ContainsKey(adsh))
                sub = repository.GetSubmission(adsh);
            else
            {
                sub = new EdgarDatasetSubmission();
                sub.ADSH = adsh;

                sub.Registrant = ParseRegistrant(repository, fields[fieldNames.IndexOf("cik")], fieldNames, fields);

                sub.Form = repository.GetSECForm(fields[fieldNames.IndexOf("form")]);

                string period = fields[fieldNames.IndexOf("period")];
                sub.Period = new DateTime(int.Parse(period.Substring(0, 4)), int.Parse(period.Substring(4, 2)), int.Parse(period.Substring(6, 2)));

                sub.Detail = fields[fieldNames.IndexOf("period")] == "1";

                sub.XBRLInstance = fields[fieldNames.IndexOf("instance")];

                sub.NumberOfCIKs = int.Parse(fields[fieldNames.IndexOf("nciks")]);

                string value = fields[fieldNames.IndexOf("aciks")];
                sub.AdditionalCIKs = String.IsNullOrEmpty(value) ? null : value;

                value = fields[fieldNames.IndexOf("pubfloatusd")];
                sub.PubFloatUSD = string.IsNullOrEmpty(value) ? (float?)null : float.Parse(value, CultureInfo.GetCultureInfo("en-us").NumberFormat);

                string floatdate = fields[fieldNames.IndexOf("floatdate")];
                sub.FloatDate = String.IsNullOrEmpty(floatdate) ? (DateTime?)null : new DateTime(int.Parse(floatdate.Substring(0, 4)), int.Parse(floatdate.Substring(4, 2)), int.Parse(floatdate.Substring(6, 2)));

                value = fields[fieldNames.IndexOf("floataxis")];
                sub.FloatAxis = String.IsNullOrEmpty(value) ? null : value;

                value = fields[fieldNames.IndexOf("floatmems")];
                sub.FloatMems = string.IsNullOrEmpty(value) ? (int?)null : int.Parse(value);

                sub.LineNumber = lineNumber;
            }
            return sub;
        }


        private Registrant ParseRegistrant(IAnalystRepository repository,string cik, List<string> fieldNames, List<string> fields)
        {
            Registrant r = repository.GetRegistrant(cik);
            if (r == null)
            {
                r = new Company();//TODO: no todos los registrantes son companias, algunos son mutual fund
                r.CIK = int.Parse(cik);
                r.Name = fields[fieldNames.IndexOf("name")];
                string value = fields[fieldNames.IndexOf("sic")];
                r.SIC = string.IsNullOrEmpty(value) ? null : repository.GetSIC(value);
                value = fields[fieldNames.IndexOf("countryba")];
                r.CountryBA = String.IsNullOrEmpty(value) ? null : value;
                value = fields[fieldNames.IndexOf("cityba")];
                r.CityBA = String.IsNullOrEmpty(value) ? null : value;
                value = fields[fieldNames.IndexOf("countryinc")];
                r.CountryInc = String.IsNullOrEmpty(value) ? null : value;
                value = fields[fieldNames.IndexOf("ein")];
                r.EIN = string.IsNullOrEmpty(value) ? (int?)null : int.Parse(value);
                r.AFS = fields[fieldNames.IndexOf("afs")];
                r.WKSI = fields[fieldNames.IndexOf("wksi")] == "1";
                value = fields[fieldNames.IndexOf("fye")];
                r.FYE = string.IsNullOrEmpty(value) ? null : value;
                repository.Add(r);
            }
            return r;
        }


        public override IList<EdgarTuple> GetKeys(IAnalystRepository repository, int datasetId)
        {
            return repository.GetSubmissionKeys(datasetId);
        }
    }
}
