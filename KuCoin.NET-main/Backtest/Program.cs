// See https://aka.ms/new-console-template for more information

using Valloon.Kucoin;
using System.Globalization;
using System;
using static System.Formats.Asn1.AsnWriter;
using System.Threading;
using System.Diagnostics;

CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
Thread.CurrentThread.CurrentCulture = culture;
Thread.CurrentThread.CurrentUICulture = culture;

var paramArray = Environment.GetCommandLineArgs();

if (paramArray.Length < 2)
{
    Logger.WriteLine($"Invalid params...", ConsoleColor.Red);
    return;
}

if (paramArray[1] == "/load")
{
    if (paramArray.Length > 3)
    {
        Loader.LoadCSV(paramArray[2], paramArray[3]);
    }
    else
    {
        Logger.WriteLine($"Invalid params...", ConsoleColor.Red);
    }
}
else if (paramArray[1] == "/test")
{
    if (paramArray.Length <= 9)
    {
        Logger.WriteLine($"Invalid params...", ConsoleColor.Red);
        return;
    }
    var symbolArray = paramArray[2].Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    decimal maxFirstCandleX1 = decimal.Parse(paramArray[3]);
    decimal startX1 = decimal.Parse(paramArray[4]);
    decimal closeX1 = decimal.Parse(paramArray[5]);
    decimal stopX1 = decimal.Parse(paramArray[6]);
    int timeout1 = int.Parse(paramArray[7]);
    decimal closeXX1 = decimal.Parse(paramArray[8]);
    int timeoutStop1 = int.Parse(paramArray[9]);
    decimal maxFirstCandleX2 = decimal.Parse(paramArray[10]);
    decimal startX2 = decimal.Parse(paramArray[11]);
    decimal closeX2 = decimal.Parse(paramArray[12]);
    decimal stopX2 = decimal.Parse(paramArray[13]);
    int timeout2 = int.Parse(paramArray[14]);
    decimal closeXX2 = decimal.Parse(paramArray[15]);
    int timeoutStop2 = int.Parse(paramArray[16]);
    decimal volume1 = decimal.Parse(paramArray[17]);
    decimal volume2 = decimal.Parse(paramArray[18]);
    Logger.WriteLine();
    Logger.WriteLine($"maxFirstCandleX1 = {maxFirstCandleX1}, startX1 = {startX1}, closeX1 = {closeX1}, stopX1 = {stopX1}, timeout1 = {timeout1}, closeXX1 = {closeXX1}, timeoutStop1 = {timeoutStop1}", ConsoleColor.DarkGray);
    Logger.WriteLine($"maxFirstCandleX2 = {maxFirstCandleX2}, startX2 = {startX2}, closeX2 = {closeX2}, stopX2 = {stopX2}, timeout2 = {timeout2}, closeXX2 = {closeXX2}, timeoutStop2 = {timeoutStop2}", ConsoleColor.DarkGray);
    Logger.WriteLine($"Symbol      \t FirstX \t First Volume \t VolumeX \t EntryPrice   \t ResultPrice \t Progress \t Timeout \t Time", ConsoleColor.White);
    decimal totalProfit = 0;
    foreach (string symbol in symbolArray)
    {
        try
        {
            (decimal firstCandleX, decimal entryPrice, decimal resultPrice, int resultTimeout, DateTime? resultDateTime, decimal? volume, decimal? volumeX) = Tester.Test(symbol, maxFirstCandleX1, startX1, closeX1, stopX1, timeout1, closeXX1, timeoutStop1, maxFirstCandleX2, startX2, closeX2, stopX2, timeout2, closeXX2, timeoutStop2, volume1, volume2);
            decimal progress = resultPrice / entryPrice - 1;
            totalProfit += progress;
            if (progress > closeX2)
                Logger.WriteLine($"{symbol}      \t {firstCandleX:F2}   \t {volume:N2} \t {volumeX:F2}   \t {entryPrice} \t {resultPrice}  \t +{progress * 100:F2} % \t {resultTimeout} \t {resultDateTime}", ConsoleColor.Green);
            else if (progress >= 0)
                Logger.WriteLine($"{symbol}      \t {firstCandleX:F2}   \t {volume:N2} \t {volumeX:F2}   \t {entryPrice} \t {resultPrice}  \t +{progress * 100:F2} % \t {resultTimeout} \t {resultDateTime}", ConsoleColor.DarkYellow);
            else
                Logger.WriteLine($"{symbol}      \t {firstCandleX:F2}   \t {volume:N2} \t {volumeX:F2}   \t {entryPrice} \t {resultPrice}  \t {progress * 100:F2} % \t {resultTimeout} \t {resultDateTime}", ConsoleColor.Red);
        }
        catch (Exception ex)
        {
            Logger.WriteLine($"{symbol}      \t {ex.Message}", ConsoleColor.DarkGray);
            //if (Debugger.IsAttached)
            //    Console.WriteLine(ex);
        }
    }
    Logger.WriteLine($"* total = {totalProfit * 100:F2}", ConsoleColor.White);
}
else
{
    Logger.WriteLine($"Invalid params...", ConsoleColor.Red);
}
