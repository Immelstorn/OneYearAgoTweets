using System;
using System.Threading;

namespace OneYearAgoTweets
{
    internal static class Program
    {
        private static readonly API Api = API.GetApi();
        private static readonly Logs Logs = Logs.GetLogsClass();

        private static void Main()
        {
            //1. Загрузили БД
            Logs.WriteLog("log.txt", "Загружаем БД");
            Api.Load();

            //1.5 Сохранили то что загрузили
            Logs.WriteLog("log.txt", "Сохранили то что загрузили");
            Api.Save();

            //2. Заснули до времени последнего твита
            while (true)
            {
                try
                {
                    Updates();
                }
                catch (Exception e)
                {
                    Logs.WriteLog("log.txt",
                                  string.Format(
                                      "\nMessage: {0}\n TargetSite: {1}\n Data: {2}\n StackTrace: {3}\n InnerException: {4}" +
                                      "\n Source: {5}\n Data: {6}\n GetBaseException: {7}\n HelpLink: {8}\n",
                                      e.Message, e.TargetSite, e.Data, e.StackTrace, e.InnerException, e.Source, e.Data,
                                      e.GetBaseException(), e.HelpLink));
                    Thread.Sleep(new TimeSpan(0, 0, 1));
                }
            }
        }

        /// <summary>
        ///     Updates.
        /// </summary>
        private static void Updates()
        {
//            for (int i = Api.OldTweets.Count - 1; i >= 0; i--)
//            {
//                if (Api.OldTweets[i].CreatedDate.ToLocalTime().AddYears(1)<DateTime.Now)
//                {
//                   Api.OldTweets.RemoveAt(i); 
//                }
//            }
            Logs.WriteLog("log.txt",
                          "Заснули до времени последнего твита " +
                          Api.OldTweets[Api.OldTweets.Count - 1].CreatedDate.ToLocalTime().AddYears(1) + "\n");

            while (DateTime.Now < Api.OldTweets[Api.OldTweets.Count - 1].CreatedDate.ToLocalTime().AddYears(1))
            {
                Thread.Sleep(new TimeSpan(0, 0, 5));
            }

            try
            {
                //3. Запостили его
                Logs.WriteLog("log.txt", "Запостили твит");
                Api.Tweet(Api.OldTweets[Api.OldTweets.Count - 1].Text);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Logs.WriteLog("log.txt",
                              string.Format(
                                  "\nMessage: {0}\n TargetSite: {1}\n Data: {2}\n StackTrace: {3}\n InnerException: {4}" +
                                  "\n Source: {5}\n Data: {6}\n GetBaseException: {7}\n HelpLink: {8}\n",
                                  e.Message, e.TargetSite, e.Data, e.StackTrace, e.InnerException, e.Source, e.Data,
                                  e.GetBaseException(), e.HelpLink));


                //если не вышло поменяли ему время на 5 минут вперед и пробуем еще раз
                Api.OldTweets[Api.OldTweets.Count - 1].CreatedDate = DateTime.Now.AddYears(-1).AddMinutes(5);
                Logs.WriteLog("log.txt", "Changed date of last tweet. Waiting 5 minutes to post.");
            }

            //4. Удалили его из БД
            Logs.WriteLog("log.txt", "Удалили его из БД");
            Api.OldTweets.RemoveAt(Api.OldTweets.Count - 1);

            //5. Получаем новые твиты
            Logs.WriteLog("log.txt", "Получаем новые твиты");
            Api.GetNewStatuses(Api.OldTweets[0].Id);
            Logs.WriteLog("log.txt", "Сохранили то что загрузили");
            Api.Save();
        }
    }
}