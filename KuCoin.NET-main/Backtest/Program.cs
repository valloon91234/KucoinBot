// See https://aka.ms/new-console-template for more information

using Valloon.Kucoin;
using System.Globalization;
using System;
using KuCoin.NET.Data.Market;
using System.Diagnostics;

CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
Thread.CurrentThread.CurrentCulture = culture;
Thread.CurrentThread.CurrentUICulture = culture;

var paramArray = Environment.GetCommandLineArgs();

//if (paramArray.Length > 2)
//{
//    Loader.LoadCSV(paramArray[1], paramArray[2]);
//}

//Loader.LoadCSV("PIX-USDT", "2022-09-20 10:00");
//Loader.LoadCSV("MPLX-USDT", "2022-09-20 00:00");
//Loader.LoadCSV("CMP-USDT", "2022-09-15 10:00");
//Loader.LoadCSV("HIODBS-USDT", "2022-09-14 12:00");
//Loader.LoadCSV("SWEAT-USDT", "2022-09-13 09:00");
//Loader.LoadCSV("NEER-USDT", "2022-08-30 12:00");
//Loader.LoadCSV("TT-USDT", "2022-09-01 09:00");
//Loader.LoadCSV("RVN-USDT", "2022-08-31 10:00");
//Loader.LoadCSV("OLE-USDT", "2022-07-07 10:00");
//Loader.LoadCSV("IHC-USDT", "2022-06-10 10:00");
//Loader.LoadCSV("HIBAYC-USDT", "2022-07-29 10:00");
//Loader.LoadCSV("MC-USDT", "2022-09-07 12:00");
//Loader.LoadCSV("FIDA-USDT", "2022-07-22 10:00");
//Loader.LoadCSV("HIENS4-USDT", "2022-08-18 10:00");

string[] symbolArray = new string[] { "PIX", "MPLX", "CMP", "HIODBS", "SWEAT", "NEER", "TT", "RVN", "OLE", "IHC", "HIBAYC", "MC", "FIDA", "HIENS4" };
//decimal startX = 0.1m;
//decimal closeX = 0.1m;
//int timeout = 5;
//decimal closeX2 = 0.02m;
//int timeoutStop = 24 * 60;

if (paramArray.Length <= 5)
{
    Logger.WriteLine($"Invalid params...", ConsoleColor.Red);
    return;
}
decimal startX = decimal.Parse(paramArray[1]);
decimal closeX = decimal.Parse(paramArray[2]);
int timeout = int.Parse(paramArray[3]);
decimal closeX2 = decimal.Parse(paramArray[4]);
int timeoutStop = int.Parse(paramArray[5]);
Logger.WriteLine();
Logger.WriteLine($"startX = {startX}, closeX = {closeX}, timeout = {timeout}, closeX2 = {closeX2}, timeoutStop = {timeoutStop}", ConsoleColor.DarkGray);
Logger.WriteLine($"Symbol \t EntryPrice   \t ResultPrice \t Progress \t Timeout \t Time", ConsoleColor.White);
decimal totalProfit = 0;
foreach (string symbol in symbolArray)
{
    (decimal entryPrice, decimal resultPrice, int resultTimeout, DateTime resultDateTime) = Tester.Test(symbol, startX, closeX, timeout, closeX2, timeoutStop);
    decimal progress = resultPrice / entryPrice - 1;
    totalProfit += progress;
    if (progress > closeX2)
        Logger.WriteLine($"{symbol} \t {entryPrice}   \t {resultPrice} \t +{progress * 100:F2} % \t {resultTimeout} \t {resultDateTime}", ConsoleColor.Green);
    else if (progress >= 0)
        Logger.WriteLine($"{symbol} \t {entryPrice}   \t {resultPrice} \t +{progress * 100:F2} % \t {resultTimeout} \t {resultDateTime}", ConsoleColor.DarkYellow);
    else
        Logger.WriteLine($"{symbol} \t {entryPrice}   \t {resultPrice} \t {progress * 100:F2} % \t {resultTimeout} \t {resultDateTime}", ConsoleColor.Red);
}
Logger.WriteLine($"* total = {totalProfit * 100:F2}", ConsoleColor.White);
