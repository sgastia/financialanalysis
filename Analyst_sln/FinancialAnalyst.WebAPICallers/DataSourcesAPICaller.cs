﻿using FinancialAnalyst.Common.Entities;
using FinancialAnalyst.Common.Entities.Assets;
using FinancialAnalyst.Common.Entities.Portfolios;
using FinancialAnalyst.Common.Entities.Prices;
using FinancialAnalyst.Common.Entities.RequestResponse;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace FinancialAnalyst.WebAPICallers
{
    public class DataSourcesAPICaller
    {
        public static APIResponse<Stock> GetCompleteStockData(string ticker, Exchange? market, AssetClass? assetType, bool includeOptionChain, bool includeFinancialData)
        {
            string uri = $"api/DataSources/getcompletestockdata?ticker={ticker}";
            if (market.HasValue)
                uri += $"&market={market.ToString()}";
            if(assetType.HasValue)
                uri += $"&assetType={assetType.ToString()}";
            else
                uri += $"&assetType={AssetClass.Unknown.ToString()}";
            uri += $"&includeOptionChain={includeOptionChain}";
            uri += $"&includeFinancialData={includeFinancialData}";
            HttpStatusCode statusCode = HttpClientWebAPI.Get(uri, out string jsonResponse);
            return JsonConvert.DeserializeObject<APIResponse<Stock>>(jsonResponse);
        }

        public static APIResponse<PriceList> GetPrices(string ticker, Exchange? exchange, DateTime? from, DateTime? to, PriceInterval interval)
        {
            string uri = $"api/DataSources/getprices?ticker={ticker}";
            if (exchange.HasValue)
                uri += $"&market={exchange.ToString()}";

            if (from.HasValue)
                uri += $"&from={from.Value.ToString("")}";

            if (to.HasValue)
                uri += $"&to={to.Value.ToString("")}";

            uri += $"&interval={interval.ToString("")}";
            HttpStatusCode statusCode = HttpClientWebAPI.Get(uri, out string jsonResponse);
            return JsonConvert.DeserializeObject<APIResponse<PriceList>>(jsonResponse);

        }

        public static HistoricalPrice GetLastPrice(string ticker, Exchange? exchange, AssetClass? assetType)
        {
            string uri = $"api/DataSources/getlastprice?ticker={ticker}";
            if (exchange.HasValue)
                uri += $"&market={exchange.ToString()}";
            if (assetType.HasValue)
                uri += $"&assetType={assetType.ToString()}";
            else
                uri += $"&assetType={AssetClass.Unknown.ToString()}";

            HttpStatusCode statusCode = HttpClientWebAPI.Get(uri, out string jsonResponse);
            var response = JsonConvert.DeserializeObject<APIResponse<HistoricalPrice>>(jsonResponse);
            if (response.Ok)
                return response.Content;
            else
                return null;
        }
    }
}
