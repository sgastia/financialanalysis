﻿using Analyst.Domain.Edgar;
using Analyst.Domain.Edgar.Datasets;
using Analyst.Services.EdgarDatasetServices;
using Analyst.Services.EdgarServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Analyst.Web.Controllers.Edgar.Datasets
{
    [RoutePrefix("edgar")]
    public class EdgarController : Controller
    {
        private const string VIEW_ROOT_PATH = "~/Views/Edgar/";
        private IEdgarService edgarService;
        private IEdgarDatasetService datasetService;

        public EdgarController(IEdgarService edgarService, IEdgarDatasetService datasetService)
        {
            this.edgarService = edgarService;
            this.datasetService = datasetService;
        }

        [HttpGet]
        [Route("home")]
        public ActionResult Index()
        {
            return View(VIEW_ROOT_PATH + "EdgarHome.cshtml");
        }

        #region Datasets
        [HttpGet]
        [Route("datasets/process")]
        public ActionResult IndexDatasets()
        {
            return View(VIEW_ROOT_PATH + "Datasets/ProcessDatasets.cshtml");
        }


        [HttpGet]
        [Route("datasets/readme")]
        public ActionResult Readme()
        {
            return View(VIEW_ROOT_PATH + "Datasets/readme.cshtml");
        }
        #endregion

        [HttpGet]
        [Route("files/home")]
        public ActionResult IndexFiles()
        {
            return View(VIEW_ROOT_PATH + "Files/EdgarFilesHome.cshtml");
        }

        #region Related data

        [HttpGet]
        [Route("secforms")]
        public ActionResult GetSecForms()
        {
            IList<SECForm> forms = edgarService.GetSECForms();
            ViewBag.Title = "SEC forms (filing type)";
            return View(VIEW_ROOT_PATH + "RelatedData/SECForms.cshtml", forms);
        }

        [HttpGet]
        [Route("sics")]
        public ActionResult GetSICs()
        {
            IList<SIC> sics = edgarService.GetSICs();
            ViewBag.Title = "Standard Industrial Classification codes";
            return View(VIEW_ROOT_PATH + "RelatedData/SICs.cshtml", sics);
        }
        #endregion

    }
}