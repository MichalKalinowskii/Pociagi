using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;

namespace Pociagi
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var date = DateTime.Now.ToString("dd.MM.yy");
            string url = $"https://rozklad-pkp.pl/pl/sq?maxJourneys=10&start=yes&dirInput=&GUIREQProduct_0=on&GUIREQProduct_1=on&GUIREQProduct_2=on&GUIREQProduct_3=on&advancedProductMode=&boardType=dep&input=&input=Bydgoszcz+Politechnika&date={date}&dateStart=18.01.25&REQ0JourneyDate=18.01.25&time=23%3A59";
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            List<TrainSchedule> schedules = new List<TrainSchedule>();

            var oddsRows = htmlDocument.DocumentNode.SelectNodes("//tr[@class='odd']");
            var evenRows = htmlDocument.DocumentNode.SelectNodes("//tr[@class='even']");

            schedules.AddRange(DecodeRows(oddsRows));
            schedules.AddRange(DecodeRows(evenRows));
        }


        public static List<TrainSchedule> DecodeRows(HtmlNodeCollection rows)
        {
            if (rows != null)
            {
                List<TrainSchedule> schedules = new();

                foreach (var row in rows)
                {
                    var trainNameNode = row.SelectSingleNode(".//span[@class='train-name']");
                    var schedule = new TrainSchedule
                    {
                        TrainName = trainNameNode.InnerText.Trim(),
                    };

                    var time = row.SelectSingleNode(".//span[@class='time']");
                    schedule.Time = time.InnerText.Trim();

                    var scheduleNode = row.SelectNodes(".//a[starts-with(@href, '/pl/sq?')]");
                    string scheduleString = string.Empty;

                    foreach (var node in scheduleNode)
                    {
                        var decodedHtml = WebUtility.HtmlDecode(node.InnerHtml).Replace("\n", string.Empty);
                        if (schedule.Direction is null)
                        {
                            schedule.Direction = decodedHtml;
                            continue;
                        }

                        if (scheduleNode.Last().Equals(node))
                        {
                            scheduleString += decodedHtml;
                            continue;
                        }

                        scheduleString += decodedHtml + " -> ";
                    }

                    schedule.Schedule = scheduleString;

                    schedules.Add(schedule);
                }

                return schedules;
            }

            return new List<TrainSchedule>();
        }
    }

}