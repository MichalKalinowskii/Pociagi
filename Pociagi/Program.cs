using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Pociagi
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var trainScheduleObservable = CreateTrainScheduleObservable();

            do { 
                await trainScheduleObservable
                    .Do(schedule => Console.WriteLine($"Pociąg: {schedule.TrainName}, Czas: {schedule.Time}, Kierunek: {schedule.Direction}, Rozkład: {schedule.Schedule}"))
                    .ToList()
                    .ToTask();
                Console.WriteLine("\n#########################################\n");
                Thread.Sleep(30000);

            } while (true);
        }

        private static IObservable<TrainSchedule> CreateTrainScheduleObservable()
        {
            var date = DateTime.Now.ToString("dd.MM.yy");
            string url = $"https://rozklad-pkp.pl/pl/sq?maxJourneys=10&start=yes&dirInput=&GUIREQProduct_0=on&GUIREQProduct_1=on&GUIREQProduct_2=on&GUIREQProduct_3=on&advancedProductMode=&boardType=dep&input=&input=Bydgoszcz+Politechnika&date={date}&dateStart=18.01.25&REQ0JourneyDate=18.01.25&time=23%3A59";

            return Observable.Using(
                () => new HttpClient(),
                client => Observable.FromAsync(() => client.GetStringAsync(url))
                    .Select(html =>
                    {
                        var htmlDocument = new HtmlDocument();
                        htmlDocument.LoadHtml(html);
                        return htmlDocument;
                    })
                    .SelectMany(doc =>
                    {
                        var oddRows = doc.DocumentNode.SelectNodes("//tr[@class='odd']");
                        var evenRows = doc.DocumentNode.SelectNodes("//tr[@class='even']");

                        var oddSchedules = DecodeRows(oddRows);
                        var evenSchedules = DecodeRows(evenRows);

                        return oddSchedules.Concat(evenSchedules);
                    })
            );
        }

        private static IEnumerable<TrainSchedule> DecodeRows(HtmlNodeCollection rows)
        {
            if (rows == null) yield break;

            foreach (var row in rows)
            {
                var trainNameNode = row.SelectSingleNode(".//span[@class='train-name']");
                var timeNode = row.SelectSingleNode(".//span[@class='time']");
                var scheduleNodes = row.SelectNodes(".//a[starts-with(@href, '/pl/sq?')]");

                if (trainNameNode == null || timeNode == null || scheduleNodes == null) continue;

                TimeOnly.TryParse(timeNode.InnerText.Trim(), out var time);

                if (time < TimeOnly.FromDateTime(DateTime.Now)) continue;

                var schedule = new TrainSchedule
                {
                    TrainName = trainNameNode.InnerText.Trim(),
                    Time = time.ToString("HH:mm")
                };

                var scheduleString = string.Empty;
                foreach (var node in scheduleNodes)
                {
                    var decodedHtml = WebUtility.HtmlDecode(node.InnerHtml).Replace("\n", string.Empty);

                    if (schedule.Direction == null)
                    {
                        schedule.Direction = decodedHtml;
                        continue;
                    }

                    if (scheduleNodes.Last().Equals(node))
                    {
                        scheduleString += decodedHtml;
                        continue;
                    }

                    scheduleString += decodedHtml + " -> ";
                }

                schedule.Schedule = scheduleString;
                yield return schedule;
            }
        }
    }
}