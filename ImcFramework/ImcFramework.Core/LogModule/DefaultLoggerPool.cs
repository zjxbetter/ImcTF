﻿using ImcFramework.Infrastructure;
using ImcFramework.WcfInterface;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComonLog = global::Common.Logging;

namespace ImcFramework.Core
{
    public class DefaultLoggerPool : ILoggerPool
    {
        private object lockObject = new object();
        private static HashSet<Tuple<string, ComonLog.LogLevel>> hashLogFile = new HashSet<Tuple<string, ComonLog.LogLevel>>();

        public DefaultLoggerPool(EServiceType serviceType)
        {
            this.ServiceType = serviceType;
        }

        public EServiceType ServiceType
        {
            get;
            private set;
        }

        //ServiceType/AllLog__Level__Date.txt
        //ServiceType/SellerAccount__Level__Date.txt
        public string GetAppenderName(string sellerAccount, ComonLog.LogLevel logLevel)
        {
            string appenderFormat = ServiceType.ToString() + "/" + "{0}{1}";
            var appenderName = string.Empty;
            if (string.IsNullOrEmpty(sellerAccount))
            {
                appenderName = string.Format(appenderFormat, "AllLog" + Defaults.BusinessLogFileSplitChar, logLevel.ToString());
            }
            else
            {
                appenderName = string.Format(appenderFormat, sellerAccount + Defaults.BusinessLogFileSplitChar, logLevel.ToString());
            }

            lock (lockObject)
            {
                var tuple = Tuple.Create<string, ComonLog.LogLevel>(sellerAccount, logLevel);
                if (!hashLogFile.Contains(tuple) || !LogFileExist(GetLogFileName(appenderName)))
                {
                    InitMainBusinessLogger(appenderName, logLevel);
                    hashLogFile.Add(tuple);
                }
            }

            return appenderName;
        }

        #region Get Logger's

        public ComonLog.ILog GetMainBusinessLogger()
        {
            return GetMainBusinessLogger(string.Empty);
        }

        public ComonLog.ILog GetMainBusinessLogger(string sellerAccount, ComonLog.LogLevel logLevel = ComonLog.LogLevel.Info)
        {
            var appenderName = GetAppenderName(sellerAccount, logLevel);
            return ComonLog.LogManager.GetLogger(appenderName);
        }

        #endregion

        #region 初始化

        private void InitMainBusinessLogger(string appenderName, ComonLog.LogLevel logLevel)
        {
            var logForSpecialAppender = (Logger)LogManager.GetLogger(appenderName).Logger;
            if (logForSpecialAppender.Appenders.Count > 0) return;   //避免重复生成日志文件

            var logFileName = "Log/" + GetLogFileName(appenderName);
            var appender = CreateAppender(appenderName, logFileName, logLevel);
            logForSpecialAppender.AddAppender(appender);
        }

        private string GetLogFileName(string appenderName)
        {
            var logFileName = string.Format("{0}" + Defaults.BusinessLogFileSplitChar + ".txt", appenderName);
            return logFileName;
        }

        private bool LogFileExist(string fileName)
        {
            var fullName = Defaults.RootDirectory + fileName.Replace('/', '\\').Replace(".txt", DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
            //Console.WriteLine(fullName);
            return File.Exists(fullName);
        }

        #endregion

        #region Privates

        private RollingFileAppender CreateAppender(string appenderName, string fileName, ComonLog.LogLevel logLevel)
        {
            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%d %-5p  %m%n";
            layout.ActivateOptions();

            RollingFileAppender appender = new RollingFileAppender();
            appender.Layout = layout;

            appender.Name = appenderName;
            appender.File = fileName;

            appender.RollingStyle = RollingFileAppender.RollingMode.Composite;
            //appender.Encoding = Encoding.Unicode;
            appender.AppendToFile = true;
            appender.MaximumFileSize = "4MB";
            appender.MaxSizeRollBackups = 100;
            appender.DatePattern = "yyyy-MM-dd";
            appender.PreserveLogFileNameExtension = true;
            appender.StaticLogFileName = false;
            appender.Threshold = ConvertLogLevel(logLevel);

            log4net.Filter.LevelRangeFilter levfilter = new log4net.Filter.LevelRangeFilter();
            levfilter.LevelMax = appender.Threshold;
            levfilter.LevelMin = appender.Threshold;
            levfilter.ActivateOptions();

            appender.AddFilter(levfilter);

            appender.ActivateOptions();

            return appender;
        }

        private SmtpAppender CreateSmtpAppender(string appenderName, string subject, string mailFrom, string mailTo, string userName, string password)
        {
            var appender = new SmtpAppender();
            appender.Name = appenderName;

            appender.Authentication = SmtpAppender.SmtpAuthentication.Basic;
            appender.To = mailFrom;
            appender.From = mailTo;
            appender.Username = userName;
            appender.Password = password;
            appender.Subject = subject;
            appender.SmtpHost = "smtp.qq.com";
            appender.BufferSize = 4;
            appender.Lossy = true;
            appender.Evaluator = new LevelEvaluator() { Threshold = Level.Error };

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%newline%date [%thread] %-5level %logger [%property{NDC}] - %message%newline%newline%newline";
            layout.ActivateOptions();

            appender.Layout = layout;

            appender.ActivateOptions();

            return appender;
        }

        private Level ConvertLogLevel(ComonLog.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case ComonLog.LogLevel.All:
                    return Level.All;
                case ComonLog.LogLevel.Debug:
                    return Level.Debug;
                case ComonLog.LogLevel.Error:
                    return Level.Error;
                case ComonLog.LogLevel.Fatal:
                    return Level.Fatal;
                case ComonLog.LogLevel.Info:
                    return Level.Info;
                case ComonLog.LogLevel.Off:
                    return Level.Off;
                case ComonLog.LogLevel.Trace:
                    return Level.Trace;
                case ComonLog.LogLevel.Warn:
                    return Level.Warn;
                default:
                    throw new NotSupportedException("Not Support logLevel:" + logLevel.ToString());
            }
        }

        #endregion
    }
}
