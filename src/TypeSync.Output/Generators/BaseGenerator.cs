﻿using System.Text;

namespace TypeSync.Output.Generators
{
    public abstract class BaseGenerator
    {
        protected string AutogeneratedHeader()
        {
            var sb = new StringBuilder();

            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// \tThis code was generated by TypeSync.");
            sb.AppendLine("//");
            sb.AppendLine("// \tChanges to this file may cause incorrect behavior and will be lost if the code is regenerated.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}