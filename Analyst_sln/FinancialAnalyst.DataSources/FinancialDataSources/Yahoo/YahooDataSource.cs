﻿using FinancialAnalyst.Common.Entities;
using FinancialAnalyst.Common.Entities.Assets;
using FinancialAnalyst.Common.Entities.Prices;
using FinancialAnalyst.Common.Interfaces;
using FinancialAnalyst.Common.Interfaces.ServiceLayerInterfaces.DataSources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace FinancialAnalyst.DataSources.Yahoo
{
    /// <summary>
    /// Thanks to:
    /// http://www.jarloo.com/get-yahoo-finance-api-data-via-yql/
    /// </summary>
    public class YahooDataSource : IPricesDataSource
    {
        private static DateTime FIRST_DATE = new DateTime(1927, 12, 30, 0, 0, 0);
        private static DateTime DATE_1970 = new DateTime(1970, 1, 1, 0, 0, 0);

        public bool TryGetPrices(string ticker, Exchange? exchange, DateTime? from, DateTime? to, PriceInterval priceInterval, out PriceList prices, out string errorMessage)
        {

            double fromValue;
            if(from.HasValue)
            {
                fromValue = (from.Value - DATE_1970).TotalSeconds;
            }
            else
            {
                fromValue = (FIRST_DATE - DATE_1970).TotalSeconds;
            }

            double toValue;
            if (to.HasValue)
            {
                toValue = (to.Value - DATE_1970).TotalSeconds;
            }
            else
            {
                toValue = (DateTime.Now - DATE_1970).TotalSeconds;
            }


            string content = YahooApiCaller.GetPrices(ticker, fromValue, toValue, priceInterval);
            string[] lines = content.Split('\n');
            prices = new PriceList();
            for (int i = 1; i < lines.Length; i++)
            {
                Price p = Price.From(lines[i]);
                prices.Add(p);
            }
            errorMessage = "ok";
            return true;
        }

        
    }
}