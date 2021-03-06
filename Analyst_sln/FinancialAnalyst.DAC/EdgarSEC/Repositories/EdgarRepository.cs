﻿using FinancialAnalyst.Common.Entities;
using FinancialAnalyst.Common.Entities.EdgarSEC;
using FinancialAnalyst.Common.Entities.EdgarSEC.Datasets;
using FinancialAnalyst.Common.Entities.EdgarSEC.Indexes;
using FinancialAnalyst.Common.Entities.EdgarSEC.Repositories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FinancialAnalyst.DataAccess.EdgarSEC.Contexts;

namespace FinancialAnalyst.DataAccess.EdgarSEC.Repositories
{
    public class EdgarRepository : IEdgarDatasetsRepository, IEdgarFilesRepository, IEdgarRepository
    {
        public const int DEFAULT_CONN_TIMEOUT = 180;
        private ConcurrentDictionary<string, SECForm> secFormsCache = new ConcurrentDictionary<string, SECForm>();
        private ConcurrentDictionary<int, Registrant> registrantsCache = new ConcurrentDictionary<int, Registrant>();
        private EdgarContext Context;

        #region Constructors
        public EdgarRepository() : this(new EdgarContext())
        {
        }

        internal EdgarRepository(EdgarContext context)
        {
            this.Context = context;
            int timeout;
            if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["ef_conn_timeout"]))
                timeout = DEFAULT_CONN_TIMEOUT;
            else
                timeout = Convert.ToInt32(ConfigurationManager.AppSettings["ef_conn_timeout"]);

            this.Context.Database.SetCommandTimeout(timeout);
        }
        #endregion

        #region IAnalystEdgarRepository implementations
        public bool ContextConfigurationAutoDetectChangesEnabled
        {
            //get { return Context.Configuration.AutoDetectChangesEnabled; }
            //set { Context.Configuration.AutoDetectChangesEnabled = value; }
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        public void Add(SECForm sf)
        {
            Context.SECForms.Add(sf);
            Context.SaveChanges();
        }
        public void Add(SIC sic)
        {
            Context.SICs.Add(sic);
            Context.SaveChanges();
        }
        public IList<TEntity> Get<TEntity>() where TEntity : class, IEdgarEntity
        {
            return GetQuery<TEntity>().ToList<TEntity>();
        }
        public int GetSECFormsCount()
        {
            return Context.SECForms.Count();
        }
        public int GetSICCount()
        {
            return Context.SICs.Count();
        }
        public SECForm GetSECForm(string code)
        {
            if (secFormsCache.ContainsKey(code))
                return secFormsCache[code];

            SECForm form = Context.SECForms.Where(x => x.Code == code).SingleOrDefault();
            if (form == null)
            {
                form = new SECForm() { Code = code };
                Context.SECForms.Add(form);
                Context.SaveChanges();
            }
            secFormsCache.TryAdd(code, form);
            return form;
        }
        public SIC GetSIC(string code)
        {
            int iCode = short.Parse(code);
            return Context.SICs.Where(x => x.Code == iCode).SingleOrDefault();
        }
        public Registrant GetRegistrant(string cik)
        {
            int iCik = int.Parse(cik);
            if (registrantsCache.ContainsKey(iCik))
                return registrantsCache[iCik];

            return Context.Registrants.Where(x => x.CIK == iCik).SingleOrDefault();
        }
        public Registrant GetRegistrant(int cik, string name)
        {
            if (registrantsCache.ContainsKey(cik))
                return registrantsCache[cik];

            Registrant r = Context.Registrants.Where(x => x.CIK == cik).SingleOrDefault();
            if (r == null)
            {
                r = new Registrant() { CIK = cik, Name = name };
                Context.Registrants.Add(r);
                Context.SaveChanges();
            }
            registrantsCache.TryAdd(cik, r);
            return r;
        }
        public IQueryable<Registrant> GetRegistrants(string sortOrder, string searchString, int pagesize, out int total)
        {
            //todo: pending to add sortOrder
            total = Context.Registrants.Where(r => r.Name.Contains(searchString)).Count();
            var query = Context.Registrants.Include("SIC").Where(r => r.Name.Contains(searchString)).OrderBy(r => r.Name);
            return query;
        }
        public IQueryable GetFilings(int cik, int? year, Quarter? quarter, string sortOrder, int pagesize, out int count)
        {
            count = 0;
            return null;
        }
        #endregion

        #region IAnalystEdgarDatasetsRepository implementations
        public int GetCount<TEntity>(int datasetId) where TEntity : class, IEdgarDatasetFile
        {
            return GetQuery<TEntity>().Where(x => x.DatasetId == datasetId).Count();
        }
        public int GetDatasetsCount()
        {
            return Context.DataSets.Count();
        }

        public EdgarDataset GetDataset(int id)
        {
            EdgarDataset ds = Context.DataSets.Where(x => x.Id == id).SingleOrDefault();
            return ds;
        }
        public EdgarDatasetTag GetTag(string tag, string version)
        {
            /*
            string sql = "select * from [dbo].[EdgarDatasetTags] where " +
                "tag = '" + tag + "' COLLATE SQL_Latin1_General_CP1_CS_AS " +
                "AND version = '" + version + "' COLLATE SQL_Latin1_General_CP1_CS_AS ";
            return Context.Tags.SqlQuery(sql).SingleOrDefault();
            */
            DbSet<EdgarDatasetTag> q = (DbSet<EdgarDatasetTag>)Context.Tags.Where(t => t.Tag == tag && t.Version == version);
            return q.SingleOrDefault();

        }
        public EdgarDatasetDimension GetDimension(string dimhash)
        {
            IQueryable<EdgarDatasetDimension> q = Context.Dimensions.Where(x => x.DimensionH == dimhash);
            string sql = q.ToString();
            return q.SingleOrDefault();
        }

        public IList<EdgarTuple> GetCalculationKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_CALCULATIONS_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetDimensionKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_DIMENSIONS_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetTagsKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_TAGS_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetSubmissionKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_SUBMISSIONS_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetNumberKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_NUMBER_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetTextKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_TEXT_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetRendersKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_RENDER_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }
        public IList<EdgarTuple> GetPresentationsKeys(int datasetId)
        {
            return Context.EdgarTuples.FromSqlRaw("exec SP_GET_PRESENTATION_KEYS @datasetid", new SqlParameter("@datasetid", datasetId)).ToList();
        }

        public void Add(Registrant r)
        {
            Context.Registrants.Add(r);
            Context.SaveChanges();
        }
        public void Add(EdgarDataset dataset)
        {
            Context.DataSets.Add(dataset);
            Context.SaveChanges();
        }
        public void Add(EdgarDataset dataset, EdgarDatasetSubmission sub)
        {

            SqlParameter ADSH = new SqlParameter("@ADSH", sub.ADSH);
            SqlParameter Period = new SqlParameter("@Period", sub.Period);
            SqlParameter Detail = new SqlParameter("@Detail", sub.Detail);
            SqlParameter XBRLInstance = new SqlParameter("@XBRLInstance", sub.XBRLInstance);
            if (sub.XBRLInstance == null) XBRLInstance.Value = DBNull.Value;
            SqlParameter NumberOfCIKs = new SqlParameter("@NumberOfCIKs", sub.NumberOfCIKs);
            SqlParameter AdditionalCIKs = new SqlParameter("@AdditionalCIKs", sub.AdditionalCIKs);
            if (sub.AdditionalCIKs == null) AdditionalCIKs.Value = DBNull.Value;
            SqlParameter PublicFloatUSD = new SqlParameter("@PublicFloatUSD", sub.PublicFloatUSD);
            if (sub.PublicFloatUSD == null) PublicFloatUSD.Value = DBNull.Value;
            SqlParameter FloatDate = new SqlParameter("@FloatDate", sub.FloatDate);
            if (sub.FloatDate == null) FloatDate.Value = DBNull.Value;
            SqlParameter FloatAxis = new SqlParameter("@FloatAxis", sub.FloatAxis);
            if (sub.FloatAxis == null) FloatAxis.Value = DBNull.Value;
            SqlParameter FloatMems = new SqlParameter("@FloatMems", sub.FloatMems);
            if (sub.FloatMems == null) FloatMems.Value = DBNull.Value;
            SqlParameter lineNumber = new SqlParameter("@LineNumber", sub.LineNumber);
            SqlParameter FormId = new SqlParameter("@Form_id", sub.Form.Id);
            SqlParameter Registrant_Id = new SqlParameter("@Registrant_Id", sub.Registrant.Id);
            SqlParameter EdgarDataset_Id = new SqlParameter("@EdgarDataset_Id", dataset.Id);


            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASSUBMISSIONS_INSERT " +
                "@ADSH, @Period, @Detail, @XBRLInstance, @NumberOfCIKs, @AdditionalCIKs, @PublicFloatUSD, @FloatDate, @FloatAxis, @FloatMems,@LineNumber, @Form_Id, @Registrant_Id, @EdgarDataset_Id",
                ADSH, Period, Detail, XBRLInstance, NumberOfCIKs, AdditionalCIKs, PublicFloatUSD, FloatDate, FloatAxis, FloatMems, lineNumber, FormId, Registrant_Id, EdgarDataset_Id);
        }
        public void Add(EdgarDataset dataset, EdgarDatasetTag tag)
        {
            SqlParameter dsid = new SqlParameter("@DataSetId", dataset.Id);
            SqlParameter tagparam = new SqlParameter("@Tag", tag.Tag);
            SqlParameter version = new SqlParameter("@Version", tag.Version);

            SqlParameter custom = new SqlParameter("@Custom", tag.Custom);
            SqlParameter abstracto = new SqlParameter("@Abstract", tag.Abstract);
            SqlParameter datatype = new SqlParameter("@Datatype", tag.ValueType);
            if (tag.ValueType == null)
                datatype.Value = DBNull.Value;
            SqlParameter LabelText = new SqlParameter("@LabelText", tag.LabelText);
            if (tag.LabelText == null)
                LabelText.Value = DBNull.Value;
            SqlParameter Documentation = new SqlParameter("@Documentation", tag.Documentation);
            if (tag.Documentation == null)
                Documentation.Value = DBNull.Value;
            SqlParameter LineNumber = new SqlParameter("@LineNumber", tag.LineNumber);

            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETTAGS_INSERT " +
                "@DataSetId, @tag,@version,@custom,@Abstract,@Datatype,@LabelText,@Documentation,@LineNumber",
                dsid, tagparam, version, custom, abstracto, datatype, LabelText, Documentation, LineNumber);
        }
        public void Add(EdgarDataset dataset, EdgarDatasetNumber number)
        {
            SqlParameter DatavalueEnddate = new SqlParameter("@DatavalueEnddate", number.DatavalueEnddate);
            SqlParameter countOfNumberOfQuarters = new SqlParameter("@CountOfNumberOfQuarters", number.CountOfNumberOfQuarters);
            SqlParameter UnitOfMeasure = new SqlParameter("@UnitOfMeasure", number.UnitOfMeasure);
            SqlParameter iprx = new SqlParameter("@IPRX", number.IPRX);
            SqlParameter value = new SqlParameter("@Value", number.Value);
            if (!number.Value.HasValue)
                value.Value = DBNull.Value;
            SqlParameter footNote;
            if (string.IsNullOrEmpty(number.FootNote))
                footNote = new SqlParameter("@FootNote", DBNull.Value);
            else
                footNote = new SqlParameter("@FootNote", number.FootNote);
            SqlParameter footLength = new SqlParameter("@FootLength", number.FootLength);
            SqlParameter numberOfDimensions = new SqlParameter("@NumberOfDimensions", number.NumberOfDimensions);
            SqlParameter coRegistrant;
            if (string.IsNullOrEmpty(number.CoRegistrant))
                coRegistrant = new SqlParameter("@CoRegistrant", DBNull.Value);
            else
                coRegistrant = new SqlParameter("@CoRegistrant", number.CoRegistrant);
            SqlParameter durp = new SqlParameter("@durp", number.Durp);
            SqlParameter datp = new SqlParameter("@datp", number.Datp);
            SqlParameter decimals = new SqlParameter("@Decimals", number.Decimals);
            SqlParameter dimensionId = new SqlParameter("@Dimension_Id", number.DimensionId);
            SqlParameter submissionId = new SqlParameter("@Submission_Id", number.SubmissionId);
            SqlParameter tagId = new SqlParameter("@Tag_Id", number.TagId);
            SqlParameter lineNumber = new SqlParameter("@LineNumber", number.LineNumber);
            SqlParameter edgarDatasetId = new SqlParameter("@EdgarDataset_Id", dataset.Id);
            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETNUMBER_INSERT " +
                "@DatavalueEnddate, @CountOfNumberOfQuarters,@UnitOfMeasure, @IPRX, @Value, @FootNote, @FootLength, @NumberOfDimensions, @CoRegistrant, @durp, @datp, @Decimals, @Dimension_Id, @Submission_Id, @Tag_Id,@LineNumber, @EdgarDataset_Id",
                DatavalueEnddate, countOfNumberOfQuarters, UnitOfMeasure, iprx, value, footNote, footLength, numberOfDimensions, coRegistrant, durp, datp, decimals, dimensionId, submissionId, tagId, lineNumber, edgarDatasetId
                );

        }
        public void Add(EdgarDataset dataset, EdgarDatasetDimension dim)
        {
            SqlParameter DimensionH = new SqlParameter("@DimensionH", SqlDbType.NVarChar, EdgarDatasetDimension.LENGHT_FIELD_DIMENSIONH);
            DimensionH.Value = dim.DimensionH;
            SqlParameter Segments = new SqlParameter("@Segments", dim.Segments);
            if (string.IsNullOrEmpty(dim.Segments))
                Segments.Value = DBNull.Value;
            SqlParameter SegmentTruncated = new SqlParameter("@SegmentTruncated", dim.SegmentTruncated);
            SqlParameter DataSetId = new SqlParameter("@DataSetId", dataset.Id);
            SqlParameter LineNumber = new SqlParameter("@LineNumber", dim.LineNumber);
            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETDIMENSIONS_INSERT " +
                "@DimensionH , @Segments, @SegmentTruncated, @DataSetId,@LineNumber",
                DimensionH, Segments, SegmentTruncated, DataSetId, LineNumber)
                ;

        }
        public void Add(EdgarDataset dataset, EdgarDatasetRender ren)
        {
            SqlParameter Report = new SqlParameter("@Report", ren.Report);
            SqlParameter RenderFile = new SqlParameter("@RenderFile", ren.RenderFileStr);
            SqlParameter MenuCategory = new SqlParameter("@MenuCategory", ren.MenuCategory);
            SqlParameter ShortName = new SqlParameter("@ShortName", ren.ShortName);
            SqlParameter LongName = new SqlParameter("@LongName", ren.LongName);

            SqlParameter Roleuri;
            if (string.IsNullOrEmpty(ren.RoleURI))
                Roleuri = new SqlParameter("@Roleuri", DBNull.Value);
            else
                Roleuri = new SqlParameter("@Roleuri", ren.RoleURI);

            SqlParameter ParentRoleuri;
            if (string.IsNullOrEmpty(ren.ParentRoleURI))
                ParentRoleuri = new SqlParameter("@ParentRoleuri", DBNull.Value);
            else
                ParentRoleuri = new SqlParameter("@ParentRoleuri", ren.ParentRoleURI);

            SqlParameter ParentReport = new SqlParameter("@ParentReport", ren.ParentReport);
            if (ren.ParentReport == null)
                ParentReport.Value = DBNull.Value;

            SqlParameter UltimateParentReport = new SqlParameter("@UltimateParentReport", ren.UltimateParentReport);
            if (ren.UltimateParentReport == null)
                UltimateParentReport.Value = DBNull.Value;

            SqlParameter Submission_Id = new SqlParameter("@Submission_Id", ren.SubmissionId);
            SqlParameter DataSetId = new SqlParameter("@DataSetId", dataset.Id);
            SqlParameter lineNumber = new SqlParameter("@LineNumber", ren.LineNumber);

            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETRENDERS_INSERT " +
                "@Report, @RenderFile, @MenuCategory, @ShortName, @LongName, @Roleuri, @ParentRoleuri, @ParentReport, @UltimateParentReport, @Submission_Id, @DataSetId, @LineNumber",
                Report, MenuCategory, ShortName, LongName, Roleuri, ParentRoleuri, ParentReport, UltimateParentReport, Submission_Id, DataSetId, lineNumber
                );
        }
        public void Add(EdgarDataset dataset, EdgarDatasetPresentation pre)
        {
            SqlParameter ReportNumber = new SqlParameter("@ReportNumber", pre.ReportNumber);
            SqlParameter Line = new SqlParameter("@Line", pre.Line);
            SqlParameter FinancialStatement = new SqlParameter("@FinancialStatement", pre.FinancialStatement);
            SqlParameter Inpth = new SqlParameter("@Inpth", pre.Inpth);
            SqlParameter RenderFile = new SqlParameter("@RenderFile", pre.RenderFileStr);
            SqlParameter PreferredLabelXBRLLinkRole = new SqlParameter("@PreferredLabelXBRLLinkRole", pre.PreferredLabelXBRLLinkRole);
            SqlParameter PreferredLabel = new SqlParameter("@PreferredLabel", pre.PreferredLabel);
            SqlParameter Negating = new SqlParameter("@Negating", pre.Negating);
            SqlParameter LineNumber = new SqlParameter("@LineNumber", pre.LineNumber);
            SqlParameter DataSetId = new SqlParameter("@DataSetId", dataset.Id);
            SqlParameter Submission_Id = new SqlParameter("@Submission_Id", pre.SubmissionId);
            SqlParameter Tag_Id = new SqlParameter("@Tag_Id", pre.TagId);
            SqlParameter Number_Id;
            if (pre.NumberId > 0)
                Number_Id = new SqlParameter("@Number_Id", pre.NumberId);
            else
                Number_Id = new SqlParameter("@Number_Id", DBNull.Value);
            SqlParameter Text_Id;
            if (pre.TextId > 0)
                Text_Id = new SqlParameter("@Text_Id", pre.TextId);
            else
                Text_Id = new SqlParameter("@Text_Id", DBNull.Value);
            SqlParameter Render_Id;
            if (pre.RenderId <= 0)
                Render_Id = new SqlParameter("@Render_Id", DBNull.Value);
            else
                Render_Id = new SqlParameter("@Render_Id", pre.RenderId);
            SqlParameter adsh_tag_version = new SqlParameter("@adsh_tag_version", pre.ADSH_Tag_Version);
            if (string.IsNullOrEmpty(pre.ADSH_Tag_Version))
                adsh_tag_version.Value = DBNull.Value;

            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETPRESENTATIONS_INSERT " +
                "@ReportNumber, @Line, @FinancialStatement, @Inpth,@RenderFile, @PreferredLabelXBRLLinkRole, @PreferredLabel, @Negating,@LineNumber, @DataSetId, @Submission_Id, @Tag_Id, @Number_Id, @Text_Id, @Render_Id, @adsh_tag_version",
                ReportNumber, Line, FinancialStatement, Inpth, PreferredLabelXBRLLinkRole, PreferredLabel, Negating, LineNumber, DataSetId, Submission_Id, Tag_Id, Number_Id, Text_Id, Render_Id, adsh_tag_version);
        }
        public void Add(EdgarDataset dataset, EdgarDatasetCalculation file)
        {
            SqlParameter LineNumber = new SqlParameter("@LineNumber", file.LineNumber);
            SqlParameter SequentialNumberForGrouping = new SqlParameter("@SequentialNumberForGrouping", file.SequentialNumberForGrouping);
            SqlParameter SequentialNumberForArc = new SqlParameter("@SequentialNumberForArc", file.SequentialNumberForArc);
            SqlParameter Negative = new SqlParameter("@Negative", file.Negative);
            SqlParameter ParentTagId = new SqlParameter("@ParentTagId", file.ParentTagId);
            SqlParameter ChildTagId = new SqlParameter("@ChildTagId", file.ChildTagId);
            SqlParameter Dataset_Id = new SqlParameter("@Dataset_Id", dataset.Id);
            SqlParameter Submission_Id = new SqlParameter("@Submission_Id", file.SubmissionId);

            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETCALC_INSERT " +
                "@LineNumber, @SequentialNumberForGrouping, @SequentialNumberForArc, @Negative, @ParentTagId, @ChildTagId, @Dataset_Id, @Submission_Id",
                LineNumber, SequentialNumberForGrouping, SequentialNumberForArc, Negative, ParentTagId, ChildTagId, Dataset_Id, Submission_Id);
        }
        public void Add(EdgarDataset dataset, EdgarDatasetText file)
        {
            SqlParameter LineNumber = new SqlParameter("@LineNumber", file.LineNumber);
            SqlParameter DatavalueEnddate = new SqlParameter("@DatavalueEnddate", file.DatavalueEnddate);
            SqlParameter CountOfNumberOfQuarters = new SqlParameter("@CountOfNumberOfQuarters", file.CountOfNumberOfQuarters);
            SqlParameter Iprx = new SqlParameter("@Iprx", file.Iprx);
            SqlParameter Language = new SqlParameter("@Language", file.Language);
            SqlParameter Dcml = new SqlParameter("@Dcml", file.Dcml);
            SqlParameter Durp = new SqlParameter("@Durp", file.Durp);
            SqlParameter Datp = new SqlParameter("@Datp", file.Datp);
            SqlParameter DimensionNumber = new SqlParameter("@DimensionNumber", file.DimensionNumber);
            if (!file.DimensionNumber.HasValue)
                DimensionNumber.Value = DBNull.Value;
            SqlParameter CoRegistrant;
            if (!string.IsNullOrEmpty(file.CoRegistrant))
                CoRegistrant = new SqlParameter("@CoRegistrant", file.CoRegistrant);
            else
                CoRegistrant = new SqlParameter("@CoRegistrant", DBNull.Value);
            SqlParameter Escaped = new SqlParameter("@Escaped", file.Escaped);
            SqlParameter SourceLength = new SqlParameter("@SourceLength", file.SourceLength);
            SqlParameter TextLength = new SqlParameter("@TextLength", file.TextLength);
            SqlParameter FootNote = new SqlParameter("@FootNote", file.FootNote);
            SqlParameter FootLength = new SqlParameter("@FootLength", file.FootLength);
            if (!file.FootLength.HasValue)
                FootLength.Value = DBNull.Value;
            SqlParameter paramContext = new SqlParameter("@Context", file.Context);
            SqlParameter Value = new SqlParameter("@Value", file.Value);
            SqlParameter Dimension_Id = new SqlParameter("@Dimension_Id", file.DimensionId);
            SqlParameter Submission_Id = new SqlParameter("@Submission_Id", file.SubmissionId);
            SqlParameter Tag_Id = new SqlParameter("@Tag_Id", file.TagId);
            SqlParameter DatasetId = new SqlParameter("@DatasetId", dataset.Id);

            Context.Database.ExecuteSqlRaw("exec SP_EDGARDATASETTEXT_INSERT " +
                "@LineNumber, @DatavalueEnddate, @CountOfNumberOfQuarters, @Iprx, @Language, @Dcml, @Durp, @Datp, @DimensionNumber, @CoRegistrant, @Escaped, @SourceLength, @TextLength, @FootNote, @FootLength, @Context, @Value, @Dimension_Id, @Submission_Id, @Tag_Id, @DatasetId",
                LineNumber, DatavalueEnddate, CountOfNumberOfQuarters, Iprx, Language, Dcml, Durp, Datp, DimensionNumber, CoRegistrant, Escaped, SourceLength, TextLength, FootNote, FootLength, paramContext, Value, Dimension_Id, Submission_Id, Tag_Id, DatasetId);

        }

        public List<int> GetMissingLines(int datasetId, DatasetsTables table, int totalLines)
        {
            string tableName = "EdgarDataset" + Enum.GetName(typeof(DatasetsTables), table);
            SqlParameter paramid = new SqlParameter("@datasetid", datasetId);
            SqlParameter paramtable = new SqlParameter("@table", tableName);
            SqlParameter paramTotal = new SqlParameter("@totallines", totalLines);

            //return Context.Database.SqlQuery<int>("exec GET_MISSING_LINE_NUMBERS @datasetid,@table,@totalLines", paramid, paramtable, paramTotal).ToList();

            var conn = Context.Database.GetDbConnection();
            var command = conn.CreateCommand();
            command.CommandText = "exec GET_MISSING_LINE_NUMBERS @datasetid,@table,@totalLines";
            command.Parameters.Add(paramid);
            command.Parameters.Add(paramtable);
            command.Parameters.Add(paramTotal);
            conn.Open();
            var reader = command.ExecuteReader();
            List<int> lines = new List<int>();
            while (reader.Read())
            {
                int line = reader.GetInt32(0);
                lines.Add(line);
            }
            conn.Close();
            return lines;

        }
        public void UpdateEdgarDataset(EdgarDataset dataset, string property)
        {
            Context.Entry<EdgarDataset>(dataset).Property(property).IsModified = true;
            Context.SaveChanges();
        }
        public void EnablePresentationIndexes(bool enable)
        {
            SqlParameter enableParam = new SqlParameter("@enable", enable);
            Context.Database.ExecuteSqlRaw("exec SP_DISABLE_PRESENTATION_INDEXES @enable", enableParam);
        }
        #endregion

        #region IAnalystEdgarFilesRepository implementations
        public void Add(MasterIndex index)
        {
            Context.MasterIndexes.Add(index);
            Context.SaveChanges();
        }
        public IList<MasterIndex> GetFullIndexes()
        {
            return Context.MasterIndexes.ToList();
        }
        public MasterIndex GetFullIndex(ushort year, Quarter quarter)
        {
            return Context.MasterIndexes.Where(index => index.Year == year && index.Quarter == quarter).SingleOrDefault();
        }
        public void Update(MasterIndex index, string property)
        {
            Context.Entry<MasterIndex>(index).Property(property).IsModified = true;
            Context.SaveChanges();

        }
        public long GetIndexEntriesCount(MasterIndex index)
        {
            return Context.IndexEntries.Where(entry => entry.MasterIndexId == index.Id).Count();
        }
        #endregion

        /*
        private ObjectQuery<TEntity> GetQuery<TEntity>() where TEntity : class, IEdgarEntity
        {
            string key = typeof(TEntity).Name;
            IObjectContextAdapter adapter = (IObjectContextAdapter)Context;
            ObjectContext objectContext = adapter.ObjectContext;
            // 1. we need the container for the conceptual model
            EntityContainer container = objectContext.MetadataWorkspace.GetEntityContainer(
                objectContext.DefaultContainerName, System.Data.Entity.Core.Metadata.Edm.DataSpace.CSpace);
            // 2. we need the name given to the element set in that conceptual model
            string name = container.BaseEntitySets.Where((s) => s.ElementType.Name.Equals(key)).FirstOrDefault().Name;
            // 3. finally, we can create a basic query for this set
            ObjectQuery<TEntity> query = objectContext.CreateQuery<TEntity>("[" + name + "]");
            return query;
        }
        */

        private IEnumerable<TEntity> GetQuery<TEntity>() where TEntity: class, IEdgarEntity
        {
            string key = typeof(TEntity).Name;
            IEnumerable<EntityEntry> entities = Context.ChangeTracker.Entries().Where(entityEntry => entityEntry.Entity is TEntity);
            return entities.Cast<TEntity>();
        }

 
        #region IDisposable implementation
        private bool isDisposed;
        private IntPtr nativeResource = IntPtr.Zero;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // The bulk of the clean-up code is implemented in Dispose(bool)

            if (isDisposed)
                return;

            if (disposing)
            {
                // free managed resources
                if (this.Context != null)
                    this.Context.Dispose();
            }

            // free native resources if there are any.
            if (nativeResource != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(nativeResource);
                nativeResource = IntPtr.Zero;
            }

            isDisposed = true;
        }
        #endregion
    }
}
