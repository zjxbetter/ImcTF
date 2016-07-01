﻿using System.Collections.Generic;
using ImcFramework.Ioc;
using System.Linq;
using ImcFramework.Core.MutilUserProgress;

namespace ImcFramework.Core
{
    /// <summary>
    /// 服务启动
    /// </summary>
    public static class ServiceManager
    {
        private static IEnumerable<IModuleExtension> extensionModules;
        private static IEnumerable<IServiceModule> buildInModules;

        private static IIocManager iocManager = null;

        static ServiceManager()
        {
            ServiceContext = new ServiceContext();
            ServiceContext.Scheduler = null;
            ServiceContext.WcfHost = null;
            ServiceContext.ProgressInfoManager = ProgressInfoManager.Instance;

            iocManager = IocManager.Instance;

            Initialize();
        }

        public static ServiceContext ServiceContext { get; set; }

        private static void Initialize()
        {
            iocManager.RegisterAssemblyAsInterfaces(typeof(ServiceManager).Assembly);

            //iocManager.Register<IServiceModule, StdQuartzModule>(DependencyLifeStyle.Singleton, false);
            //iocManager.Register<IServiceModule, WcfServiceModule>(DependencyLifeStyle.Singleton, false);

            buildInModules = iocManager.Resolve<IEnumerable<IServiceModule>>();
            extensionModules = iocManager.Resolve<IEnumerable<IModuleExtension>>();
        }

        public static void StartAll()
        {
            foreach (var buidIn in buildInModules)
            {
                if (buidIn is IModuleExtension) continue;
                buidIn.IocManager = iocManager;
                buidIn.Initialize();
                buidIn.Start();
            }

            foreach (var item in extensionModules)
            {
                item.ServiceContext = ServiceContext;
                var svc = (item as IServiceModule);
                svc.IocManager = iocManager;
                svc.Initialize();
                svc.Start();
            }
        }

        public static void StopAll()
        {
            if (extensionModules != null)
            {
                foreach (var item in extensionModules)
                {
                    (item as IServiceModule).Stop();
                }
            }

            foreach (var buidIn in buildInModules.Reverse())
            {
                if (buidIn is IModuleExtension) continue;
                buidIn.Stop();
            }
        }
    }
}
