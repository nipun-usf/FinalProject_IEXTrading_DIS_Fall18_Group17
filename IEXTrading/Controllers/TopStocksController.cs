﻿using IEXTrading.DataAccess;
using IEXTrading.Infrastructure.IEXTradingHandler;
using IEXTrading.Models;
using IEXTrading.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IEXTrading.Controllers
{
    public class TopStocksController : Controller
    {
        public ApplicationDbContext dbContext;

        public TopStocksController(ApplicationDbContext context)
        {
            dbContext = context;
        }

        public IActionResult Index(string sector, int cap)
        {
            IEXHandler webHandler = new IEXHandler();
            Dictionary<string, float> quoteDict = new Dictionary<string, float>();
            float priceMomentum;
            Quote quoteSym;
            List<QuotesEquities> quotesEquities = new List<QuotesEquities>();
            List<Quote> filteredQuotes = new List<Quote>();

            ViewBag.dbSuccessChart = 0;

            if (sector is null && cap == 0)
            {
                sector = "Technology";
                cap = 2;
            }

            //Save sector and cap in TempData
            TempData["sector"] = sector;
            TempData["cap"] = cap;

            List<Quote> quotes = webHandler.GetQuotes(sector, cap);

            //*******52-Week Price Range Strategy*********
            //A simple way to measure price momentum is to consider the proximity of a stock to its 52-week high or low. 
            //The formula I used for this was current price minus 52-week low divided by 52-week high minus 52-week low. 
            //Stocks that score 82% or higher on this formula tend to outperform, while stocks that score 41% or lower tend to underperform. 
            foreach (Quote q in quotes)
            {
                priceMomentum = (q.latestPrice - q.week52Low) / (q.week52High - q.week52Low);
                if (priceMomentum >= 0.82)
                {
                    quoteDict.Add(q.symbol, priceMomentum);
                }
            }

            //filter out top stocks symbols - with highest priceMomentum value
            List<string> topStockSymbols = quoteDict.OrderByDescending(d => d.Value).ToDictionary(t => t.Key, v => v.Value)
                                            .Keys.ToList().GetRange(0, 5);

            foreach (string symbol in topStockSymbols)
            {
                quoteSym = quotes.Where(q => q.symbol == symbol).FirstOrDefault();
                webHandler = new IEXHandler();
                List<Equity> equities = webHandler.GetChart(symbol);
                equities = equities.OrderBy(c => c.date).ToList(); //Make sure the data is in ascending order of date.
                string dates = string.Join(",", equities.Select(e => e.date));
                string prices = string.Join(",", equities.Select(e => e.high));
                string volumes = string.Join(",", equities.Select(e => e.volume / 1000000)); //Divide vol by million
                float avgprice = equities.Average(e => e.high);
                double avgvol = equities.Average(e => e.volume) / 1000000; //Divide volume by million
                quotesEquities.Add(new QuotesEquities(quoteSym, dates, prices, volumes, avgprice, avgvol));
                filteredQuotes.Add(quoteSym);
            }

            //Save top stocks in TempData
            TempData["TopStocks"] = JsonConvert.SerializeObject(filteredQuotes);

            var myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var currentDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone);

            TempData["timestamp"] = currentDateTime;

            return View(quotesEquities);
        }

        /****
         * Saves the Top Stocks in database table Quote.
        ****/
        public IActionResult SaveTopStocks(string ssector, int scap)
        {
            List<QuotesEquities> quotesEquities = new List<QuotesEquities>();
            List<Quote> quote = JsonConvert.DeserializeObject<List<Quote>>(TempData["TopStocks"].ToString());
            IEXHandler webHandler;

            foreach (Quote q in quote)
            {
                webHandler = new IEXHandler();
                List<Equity> equities = webHandler.GetChart(q.symbol);
                equities = equities.OrderBy(c => c.date).ToList(); //Make sure the data is in ascending order of date.
                string dates = string.Join(",", equities.Select(e => e.date));
                string prices = string.Join(",", equities.Select(e => e.high));
                string volumes = string.Join(",", equities.Select(e => e.volume / 1000000)); //Divide vol by million
                float avgprice = equities.Average(e => e.high);
                double avgvol = equities.Average(e => e.volume) / 1000000; //Divide volume by million
                quotesEquities.Add(new QuotesEquities(q, dates, prices, volumes, avgprice, avgvol));

                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.Quotes.Where(c => c.symbol.Equals(q.symbol)).Count() == 0)
                {
                    dbContext.Quotes.Add(q);
                }
            }
            dbContext.SaveChanges();
            TempData["dbSuccessComp"] = 1;
            TempData["TopStocks"] = JsonConvert.SerializeObject(quote);
            return RedirectToAction("Index", new { sector = ssector, cap = scap });
        }
    }
}