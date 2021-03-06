﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using IEXTrading.Models;
using Newtonsoft.Json;

namespace IEXTrading.Infrastructure.IEXTradingHandler
{
    public class IEXHandler
    {
        static string BASE_URL = "https://api.iextrading.com/1.0/"; //This is the base URL, method specific URL is appended to this.
        HttpClient httpClient;

        public IEXHandler()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /****
         * Calls the IEX reference API to get the list of symbols. 
        ****/
        public List<Company> GetSymbols()
        {
            string IEXTrading_API_PATH = BASE_URL + "ref-data/symbols";
            string companyList = "";

            List<Company> companies = null;

            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                companyList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (!companyList.Equals(""))
            {
                companies = JsonConvert.DeserializeObject<List<Company>>(companyList);
                companies = companies.GetRange(0, 40);
                //TODO:Pick out 30 companies which have lowest PE ratio
                foreach(Company company in companies)
                {
                    IEXHandler webHandler2 = new IEXHandler();
                    Quote quote = webHandler2.GetQuote(company.symbol);
                    if (quote.peRatio > 0)
                    {
                        company.peRatio = quote.peRatio;
                    } else {
                        company.peRatio = 10000;
                    }
                }
                companies = companies.OrderBy(c => c.peRatio).ToList().GetRange(0, 20);
            }
            return companies;
        }

        /****
         * Calls the IEX stock API to get 1 year's chart for the supplied symbol. 
        ****/
        public List<Equity> GetChart(string symbol)
        {
            //Using the format method.
            //string IEXTrading_API_PATH = BASE_URL + "stock/{0}/batch?types=chart&range=1y";
            //IEXTrading_API_PATH = string.Format(IEXTrading_API_PATH, symbol);

            string IEXTrading_API_PATH = BASE_URL + "stock/" + symbol + "/batch?types=chart&range=1y";

            string charts = "";
            List<Equity> Equities = new List<Equity>();
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                charts = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!charts.Equals(""))
            {
                ChartRoot root = JsonConvert.DeserializeObject<ChartRoot>(charts, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Equities = root.chart.ToList();
            }
            //make sure to add the symbol the chart
            foreach (Equity Equity in Equities)
            {
                Equity.symbol = symbol;
            }

            return Equities;
        }

        public Quote GetQuote(string symbol)
        {
            string IEXTrading_API_PATH = BASE_URL + "stock/" + symbol + "/quote";
            string quotestr = "";
            Quote Quotes = new Quote();
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                quotestr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (!quotestr.Equals(""))
            {
                //Only need two values in this case
                string[] q = quotestr.Split(',');
                string newQ = q[0] + ',' + q[37] + '}';
                if (newQ.Split(':')[2] != "null}")
                {
                    Quotes = JsonConvert.DeserializeObject<Quote>(newQ);
                }
            }
            return Quotes;
        }


        public Financial getFinancial(string symbol)
        {
            string IEXTrading_API_PATH = BASE_URL + "/stock/" + symbol + "/financials?period=quarter";
            Financial financial = new Financial();
            string financialDetails = "";
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                financialDetails = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!financialDetails.Equals(""))
            {
                char[] delimiters = new char[] { '\"', '\n', ':', ','};
                string[] f = financialDetails.Split(delimiters);
                string s = f[4];
                string o = f[26];
                string t = f[54];
                string c = f[86];
                if (o == "null") { o = "-1000000"; } 
                if (t == "null") { t = "-1000000"; }
                if (c == "null") { t = "-1000000"; }

                financial.symbol = s;
                financial.operatingRevenue = o;
                financial.totalAssets = t;
                financial.cashFlow = c;
            }
            return financial;
        }
    }
}
