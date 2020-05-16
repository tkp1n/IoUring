using System.Collections.Generic;
using System.IO;
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
            CreateRingFunctions(functions);
            CreateRingOptionEnum(functions);
            CreateSubmissionFunctions(functions, false);
            CreateSubmissionFunctions(functions, true);
            CreateConcurrentRingFunctions(functions);
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

            foreach (var f in functions) {
                f.Parameters.Add(new Parameter
                {
                    Type = "ushort",
                    Name = "personality",
                    Default = "0",
                    Comment = "The personality to impersonate for this submission"
                });

                f.Mapping.Add("personality", "personality");
            }

            return functions;
        }

        static void CreateRingFunctions(List<Function> functions)
        {
            using StreamWriter sw = new StreamWriter("../IoUring/Ring.Submission.Generated.cs", false);

            sw.WriteLine("using IoUring.Internal;");
            sw.WriteLine("using Tmds.Linux;");
            sw.WriteLine("using static Tmds.Linux.LibC;");
            sw.WriteLine("using static IoUring.Internal.ThrowHelper;");
            sw.WriteLine("");
            sw.WriteLine("namespace IoUring");
            sw.WriteLine("{");
            sw.WriteLine("    public unsafe partial class Ring");
            sw.WriteLine("    {");
            foreach (var function in functions)
            {
                // PrepareXXX

                sw.WriteLine("        /// <summary>");
                sw.WriteLine($"        /// Adds {function.Comment} to the Submission Queue without it being submitted.");
                sw.WriteLine("        /// The actual submission can be deferred to avoid unnecessary memory barriers.");
                sw.WriteLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sw.WriteLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sw.WriteLine("        /// <exception cref=\"SubmissionQueueFullException\">If no more free space in the Submission Queue is available</exception>");
                sw.WriteLine("        /// <exception cref=\"TooManyOperationsInFlightException\">If <see cref=\"Ring.SupportsNoDrop\"/> is false and too many operations are currently in flight</exception>");

                sw.Write($"        public void Prepare{function.Name}(");
                List<string> parameters = new List<string>();
                foreach (var parameter in function.Parameters)
                {
                    StringBuilder pb = new StringBuilder();
                    pb.Append($"{parameter.Type} {parameter.Name}");
                    if (parameter.Default != null)
                        pb.Append(" = " + parameter.Default);
                    parameters.Add(pb.ToString());
                }

                sw.Write(string.Join(", ", parameters));
                sw.WriteLine(")");
                sw.WriteLine( "        {");
                sw.WriteLine($"            var result = Prepare{function.Name}Internal({string.Join(", ", function.Parameters.Select(p => p.Name))});");
                sw.WriteLine( "            if (result != SubmissionAcquireResult.SubmissionAcquired)");
                sw.WriteLine( "            {");
                sw.WriteLine( "                ThrowSubmissionAcquisitionException(result);");
                sw.WriteLine( "            }");
                sw.WriteLine( "        }");
                sw.WriteLine();

                // TryPrepareXXX
                sw.WriteLine("        /// <summary>");
                sw.WriteLine($"        /// Attempts to add {function.Comment} to the Submission Queue without it being submitted.");
                sw.WriteLine("        /// The actual submission can be deferred to avoid unnecessary memory barriers.");
                sw.WriteLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sw.WriteLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sw.WriteLine("        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>");
                sw.Write($"        public bool TryPrepare{function.Name}(");
                sw.Write(string.Join(", ", parameters));
                sw.WriteLine(")");
                sw.WriteLine( "        {");
                sw.WriteLine($"            return Prepare{function.Name}Internal({string.Join(", ", function.Parameters.Select(p => p.Name))}) == SubmissionAcquireResult.SubmissionAcquired;");
                sw.WriteLine( "        }");
                sw.WriteLine();

                // PrepareXXXInternal
                sw.Write($"        private SubmissionAcquireResult Prepare{function.Name}Internal(");
                sw.Write(string.Join(", ", parameters));
                sw.WriteLine(")");

                sw.WriteLine( "        {");
                sw.WriteLine( "            var acquireResult = NextSubmissionQueueEntry(out var sqe);");
                sw.WriteLine( "            if (acquireResult != SubmissionAcquireResult.SubmissionAcquired) return acquireResult;");
                sw.WriteLine();
                sw.WriteLine( "            unchecked");
                sw.WriteLine( "            {");

                foreach (var mapping in function.Mapping)
                {
                    sw.WriteLine($"                sqe->{mapping.Key} = {mapping.Value};");
                }

                sw.WriteLine( "            }");
                sw.WriteLine();
                sw.WriteLine( "            return SubmissionAcquireResult.SubmissionAcquired;");
                sw.WriteLine( "        }");
                sw.WriteLine();
            }

            sw.WriteLine("    }");
            sw.WriteLine("}");
        }

        static void CreateRingOptionEnum(List<Function> functions)
        {
            using StreamWriter sw = new StreamWriter("../IoUring/RingOperation.Generated.cs", false);
            sw.WriteLine("namespace IoUring");
            sw.WriteLine("{");
            sw.WriteLine("    public enum RingOperation : byte");
            sw.WriteLine("    {");

            foreach (var function in functions)
            {
                sw.WriteLine($"        {function.Name},");
            }

            sw.WriteLine("    }");
            sw.WriteLine("}");
        }

        static void CreateSubmissionFunctions(List<Function> functions, bool concurrent)
        {
            var path = concurrent
                ? "../IoUring.Concurrent/Concurrent/Submission.Generated.cs"
                : "../IoUring/Submission.Generated.cs";

            using StreamWriter sw = new StreamWriter(path, false);

            sw.WriteLine("using Tmds.Linux;");
            sw.WriteLine("using static Tmds.Linux.LibC;");
            sw.WriteLine("");
            if (concurrent)
            {
                sw.WriteLine("namespace IoUring.Concurrent");
            }
            else
            {
                sw.WriteLine("namespace IoUring");
            }
            sw.WriteLine("{");
            sw.WriteLine("    public readonly unsafe partial struct Submission");
            sw.WriteLine("    {");
            foreach (var function in functions)
            {
                sw.WriteLine("        /// <summary>");
                sw.WriteLine($"        /// Prepares this Submission Queue Entry as {function.Comment}.");
                sw.WriteLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sw.WriteLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }
                sw.Write($"        public void Prepare{function.Name}(");
                List<string> parameters = new List<string>();
                foreach (var parameter in function.Parameters)
                {
                    StringBuilder pb = new StringBuilder();
                    pb.Append($"{parameter.Type} {parameter.Name}");
                    if (parameter.Default != null)
                        pb.Append(" = " + parameter.Default);
                    parameters.Add(pb.ToString());
                }

                sw.Write(string.Join(", ", parameters));
                sw.WriteLine(")");
                sw.WriteLine( "        {");
                sw.WriteLine("            var sqe = _sqe;");
                sw.WriteLine();
                sw.WriteLine( "            unchecked");
                sw.WriteLine( "            {");

                foreach (var mapping in function.Mapping)
                {
                    sw.WriteLine($"                sqe->{mapping.Key} = {mapping.Value};");
                }

                sw.WriteLine( "            }");
                sw.WriteLine( "        }");
                sw.WriteLine();
            }

            sw.WriteLine("    }");
            sw.WriteLine("}");
        }

        static void CreateConcurrentRingFunctions(List<Function> functions)
        {
            using StreamWriter sw = new StreamWriter("../IoUring.Concurrent/Concurrent/ConcurrentRing.Generated.cs", false);
            sw.WriteLine("using IoUring.Internal;");
            sw.WriteLine("using Tmds.Linux;");
            sw.WriteLine("using static IoUring.Internal.ThrowHelper;");
            sw.WriteLine("");
            sw.WriteLine("namespace IoUring.Concurrent");
            sw.WriteLine("{");
            sw.WriteLine("    public sealed unsafe partial class ConcurrentRing : BaseRing");
            sw.WriteLine("    {");
            foreach (var function in functions)
            {
                // PrepareXXX

                sw.WriteLine("        /// <summary>");
                sw.WriteLine($"        /// Adds {function.Comment} to the Submission Queue.");
                sw.WriteLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sw.WriteLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sw.WriteLine("        /// <exception cref=\"SubmissionQueueFullException\">If no more free space in the Submission Queue is available</exception>");

                sw.Write($"        public void Prepare{function.Name}(");
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

                sw.Write(string.Join(", ", parameters));
                sw.WriteLine(")");
                sw.WriteLine( "        {");
                sw.WriteLine($"            if (!TryPrepare{function.Name}({string.Join(", ", function.Parameters.Select(p => p.Name))}))");
                sw.WriteLine( "            {");
                sw.WriteLine( "                ThrowSubmissionQueueFullException();");
                sw.WriteLine( "            }");
                sw.WriteLine( "        }");
                sw.WriteLine();

                // TryPrepareXXX
                sw.WriteLine("        /// <summary>");
                sw.WriteLine($"        /// Attempts to add {function.Comment} to the Submission Queue without it being submitted.");
                sw.WriteLine("        /// The actual submission can be deferred to avoid unnecessary memory barriers.");
                sw.WriteLine("        /// </summary>");
                foreach (var parameter in function.Parameters)
                {
                    sw.WriteLine($"        /// <param name=\"{parameter.Name}\">{parameter.Comment}</param>");
                }

                sw.WriteLine("        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>");
                sw.Write($"        public bool TryPrepare{function.Name}(");
                sw.Write(string.Join(", ", parameters));
                sw.WriteLine(")");
                sw.WriteLine( "        {");
                sw.WriteLine( "            if (!TryAcquireSubmission(out var submission))");
                sw.WriteLine( "                return false;");
                sw.WriteLine();
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
                sw.WriteLine($"            submission.Prepare{function.Name}({string.Join(", ", args)});");
                sw.WriteLine();
                sw.WriteLine( "            Release(submission);");
                sw.WriteLine( "            return true;");
                sw.WriteLine( "        }");
                sw.WriteLine();
            }

            sw.WriteLine("    }");
            sw.WriteLine("}");
        }
    }
}