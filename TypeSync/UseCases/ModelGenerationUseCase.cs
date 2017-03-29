﻿using System;
using System.IO;
using System.Linq;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using TypeSync.Core;
using TypeSync.Core.Analyzers;
using TypeSync.Core.SyntaxRewriters;

namespace TypeSync.UseCases
{
    public enum PathKind : sbyte
    {
        File,
        Project,
        Solution
    }

    public class ModelGenerationUseCase : IUseCase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ModelGenerationUseCase));

        private string _path;
        private PathKind _pathKind;

        public string Id { get; } = "GenerateModels";

        public string Name { get; } = "Generate TypeScript model classes from C# DTO objects.";

        public ModelGenerationUseCase(string path, PathKind pathKind)
        {
            _path = path;
            _pathKind = pathKind;
        }

        public void Execute()
        {
            switch (_pathKind)
            {
                case PathKind.File:
                    ExecuteOnFile();
                    break;
                case PathKind.Project:
                    ExecuteOnProject();
                    break;
                case PathKind.Solution:
                    ExecuteOnSolution();
                    break;
                default:
                    break;
            }
        }

        private void ExecuteOnFile()
        {
            // parse the syntax tree from a .cs file
            var viewModelText = File.ReadAllText(_path);

            var tree = CSharpSyntaxTree.ParseText(viewModelText).WithFilePath(_path);

            // check for any syntax errors
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

            if (errors.Any())
            {
                log.Warn("Syntax contains errors: ");

                foreach (var error in errors)
                {
                    log.Warn(error.ToString());
                }
            }

            var root = tree.GetRoot();

            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("MyCompilation", syntaxTrees: new[] { tree }, references: new[] { mscorlib });

            // note that we must specify the tree for which we want the model.
            // each tree has its own semantic model
            var semanticModel = compilation.GetSemanticModel(tree);

            var classSyntax = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var classSymbol = semanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;

            // rewrite System types with aliases
            var aliasRewriter = new PropertyTypeAliasRewriter();
            var rewriteResult = aliasRewriter.Visit(root);

            if (root != rewriteResult)
            {
                root = rewriteResult;

                File.WriteAllText(_path, root.ToFullString());
                log.Debug("Some property types were replaced with aliases");
            }

            ProccessSyntaxTree(compilation, tree, semanticModel);
        }

        private void ExecuteOnProject()
        {
            throw new NotImplementedException();
        }

        private void ExecuteOnSolution()
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(_path).Result;

            foreach (var project in solution.Projects)
            {
                Console.WriteLine(project.Name);

                var compilation = project.GetCompilationAsync().Result;
                var syntaxTrees = compilation.SyntaxTrees;

                foreach (var syntaxTree in syntaxTrees)
                {
                    if (syntaxTree.ToString().Contains("auto-generated"))
                    {
                        continue;
                    }

                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    ProccessSyntaxTree(compilation, syntaxTree, semanticModel);
                }

                foreach (var document in project.Documents)
                {
                    Console.WriteLine(document.Name);
                }
            }
        }

        private void ProccessSyntaxTree(Compilation compilation, SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            // generate the TypeScript code and output to file
            var outputter = new TypeScriptOutputter();
            var generator = new TypeScriptGenerator();
            var analyzer = new DTOAnalyzer(compilation, syntaxTree, semanticModel);

            var classModels = analyzer.AnalyzeDTOs();

            foreach (var classModel in classModels)
            {
                log.DebugFormat("Class {0}", classModel.Name);

                var contents = generator.Generate(classModel);

                log.Debug("Contents generated");

                outputter.Output(@"C:\Dev\temp\", classModel.Name, contents);

                log.Debug("Contents outputted");
            }
        }
    }
}
