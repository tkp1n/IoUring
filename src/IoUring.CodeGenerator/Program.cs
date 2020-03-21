using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Tmds.Linux;

namespace IoUring.CodeGenerator
{
    class Function
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public bool Unsafe { get; set; }
        public List<Parameter> Parameters { get; } = new List<Parameter>(); 
        public Dictionary<string, string> Mapping = new Dictionary<string, string>();
    }

    class Parameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Default { get; set; }
        public string Comment { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var functions = Parse("io_uring.xml");
            // Console.WriteLine(CreateRingFunctions(functions));
            // Console.WriteLine(CreateSubmissionFunctions(functions));
            Console.WriteLine(CreateConcurrentRingFunctions(functions));
        }

        static List<Function> Parse(string uri)
        {
            using var reader = XmlReader.Create(uri);

            var functions = new List<Function>();
            var function = new Function();
            var fieldNames = typeof(io_uring_sqe).GetFields().Select(f => f.Name);

            while (reader.Read())
            {
                if (!reader.IsStartElement()) continue;
                if (reader.Name == "Function")
                {
                    function = new Function();
                    function.Name = reader.GetAttribute("name");
                    function.Comment = reader.GetAttribute("comment");
                    function.Unsafe = reader.GetAttribute("unsafe") != null;
                    functions.Add(function);
                }

                if (reader.Name == "Parameter")
                {
                    var parameter = new Parameter();
                    parameter.Type = reader.GetAttribute("type");
                    parameter.Name = reader.GetAttribute("name");
                    parameter.Default = reader.GetAttribute("default");
                    parameter.Comment = reader.GetAttribute("comment");
                    function.Parameters.Add(parameter);
                }

                if (reader.Name == "MapToFields")
                {
                    foreach (var fieldName in fieldNames)
                    {
                        var value = reader.GetAttribute(fieldName);
                        if (value != null)
                            function.Mapping[fieldName] = value;
                    }
                }
            }

            return functions;
        }

        static string CreateRingFunctions(List<Function> functions)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var function in functions)
            {
                // PrepareXXX
                
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Adds {function.Comment} to the Submission Queue without it being submitted.");
                sb.AppendLine("        /// The actual submission can be deferred to avoid unnecessary memory barriers.");
                sb.AppendLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sb.AppendLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sb.AppendLine("        /// <exception cref=\"SubmissionQueueFullException\">If no more free space in the Submission Queue is available</exception>");

                sb.Append($"        public void Prepare{function.Name}(");
                List<string> parameters = new List<string>();
                foreach (var parameter in function.Parameters)
                {
                    StringBuilder pb = new StringBuilder();
                    pb.Append($"{parameter.Type} {parameter.Name}");
                    if (parameter.Default != null)
                        pb.Append(" = " + parameter.Default);
                    parameters.Add(pb.ToString());
                }

                sb.Append(string.Join(", ", parameters));
                sb.AppendLine(")");
                sb.AppendLine( "        {");
                sb.AppendLine($"            if (!TryPrepare{function.Name}({string.Join(", ", function.Parameters.Select(p => p.Name))}))");
                sb.AppendLine( "            {");
                sb.AppendLine( "                ThrowSubmissionQueueFullException();");
                sb.AppendLine( "            }");
                sb.AppendLine( "        }");
                sb.AppendLine();
                
                // TryPrepareXXX
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Attempts to add {function.Comment} to the Submission Queue without it being submitted.");
                sb.AppendLine("        /// The actual submission can be deferred to avoid unnecessary memory barriers.");
                sb.AppendLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sb.AppendLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sb.AppendLine("        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>");
                sb.Append($"        public bool TryPrepare{function.Name}(");
                sb.Append(string.Join(", ", parameters));
                sb.AppendLine(")");
                sb.AppendLine( "        {");
                sb.AppendLine( "            if (!NextSubmissionQueueEntry(out var sqe))");
                sb.AppendLine( "                return false;");
                sb.AppendLine();
                sb.AppendLine( "            unchecked");
                sb.AppendLine( "            {");

                foreach (var mapping in function.Mapping)
                {
                    sb.AppendLine($"                sqe->{mapping.Key} = {mapping.Value};");
                }
                
                sb.AppendLine( "            }");
                sb.AppendLine();
                sb.AppendLine( "            return true;");
                sb.AppendLine( "        }");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static string CreateSubmissionFunctions(List<Function> functions)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var function in functions)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Prepares this Submission Queue Entry as {function.Comment}.");
                sb.AppendLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sb.AppendLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }
                sb.Append($"        public void Prepare{function.Name}(");
                List<string> parameters = new List<string>();
                foreach (var parameter in function.Parameters)
                {
                    StringBuilder pb = new StringBuilder();
                    pb.Append($"{parameter.Type} {parameter.Name}");
                    if (parameter.Default != null)
                        pb.Append(" = " + parameter.Default);
                    parameters.Add(pb.ToString());
                }

                sb.Append(string.Join(", ", parameters));
                sb.AppendLine(")");
                sb.AppendLine( "        {");
                sb.AppendLine("            var sqe = _sqe;");
                sb.AppendLine();
                sb.AppendLine( "            unchecked");
                sb.AppendLine( "            {");

                foreach (var mapping in function.Mapping)
                {
                    sb.AppendLine($"                sqe->{mapping.Key} = {mapping.Value};");
                }
                
                sb.AppendLine( "            }");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static string CreateConcurrentRingFunctions(List<Function> functions)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var function in functions)
            {
                // PrepareXXX
                
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Adds {function.Comment} to the Submission Queue.");
                sb.AppendLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sb.AppendLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sb.AppendLine("        /// <exception cref=\"SubmissionQueueFullException\">If no more free space in the Submission Queue is available</exception>");

                sb.Append($"        public void Prepare{function.Name}(");
                List<string> parameters = new List<string>();
                foreach (var parameter in function.Parameters)
                {
                    if (parameter.Name == "options")
                    {
                        parameters.Add($"Concurrent{parameter.Type} {parameter.Name} = Concurrent{parameter.Default}");
                    }
                    else
                    {
                        StringBuilder pb = new StringBuilder();
                        pb.Append($"{parameter.Type} {parameter.Name}");
                        if (parameter.Default != null)
                            pb.Append(" = " + parameter.Default);
                        parameters.Add(pb.ToString());
                    }
                }

                sb.Append(string.Join(", ", parameters));
                sb.AppendLine(")");
                sb.AppendLine( "        {");
                sb.AppendLine($"            if (!TryPrepare{function.Name}({string.Join(", ", function.Parameters.Select(p => p.Name))}))");
                sb.AppendLine( "            {");
                sb.AppendLine( "                ThrowSubmissionQueueFullException();");
                sb.AppendLine( "            }");
                sb.AppendLine( "        }");
                sb.AppendLine();
                
                // TryPrepareXXX
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// Attempts to add {function.Comment} to the Submission Queue without it being submitted.");
                sb.AppendLine("        /// The actual submission can be deferred to avoid unnecessary memory barriers.");
                sb.AppendLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sb.AppendLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sb.AppendLine("        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>");
                sb.Append($"        public bool TryPrepare{function.Name}(");
                sb.Append(string.Join(", ", parameters));
                sb.AppendLine(")");
                sb.AppendLine( "        {");
                sb.AppendLine( "            if (!TryAcquireSubmission(out var submission))");
                sb.AppendLine( "                return false;");
                sb.AppendLine();
                List<string> args = new List<string>();
                foreach (var parameter in function.Parameters)
                {
                    if (parameter.Name == "options")
                    {
                        args.Add("(SubmissionOption) " + parameter.Name);
                    }
                    else
                    {
                        args.Add(parameter.Name);
                    }
                }
                sb.AppendLine($"            submission.Prepare{function.Name}({string.Join(", ", args)});");
                sb.AppendLine();
                sb.AppendLine( "            Release(submission);");
                sb.AppendLine( "            return true;");
                sb.AppendLine( "        }");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}