﻿using log4net;
using TypeSync.Models;

namespace TypeSync.UseCases.Features
{
    public class ProjectTemplateScaffoldingUseCase : IUseCase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ProjectTemplateScaffoldingUseCase));

        public UseCase Id => UseCase.ProjectTemplateScaffolding;

        public string Description => "Scaffold entire Angular project templates based on server side ASP.NET Web APIs.";

        public ProjectTemplateScaffoldingUseCase()
        {
        }

        public Result Handle()
        {
            return Result.CreateSuccess();
        }
    }
}
