namespace PSICover;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

// The CoverageAnalyzer for .Net
class Analyzer {
   public Analyzer (string dir, string runExe, params string[] modules) {
      Dir = dir; RunExe = runExe; Modules = modules.ToList ();
   }
   readonly string Dir;
   readonly string RunExe;
   readonly List<string> Modules;

   public void Run () {
      Modules.ForEach (MakeBackup);
      try {
         Modules.ForEach (Disassemble);
         Modules.ForEach (AddInstrumentation);
         Modules.ForEach (Assemble);
         RunCode ();
         GenerateOutputs ();
      } finally {
         Modules.ForEach (RestoreBackup);
      }
   }

   // Make backups of the DLL and PDB files
   void MakeBackup (string module) {
      Console.WriteLine ("Making backups");
      Directory.CreateDirectory ($"{Dir}/Backups");
      File.Copy ($"{Dir}/{module}", $"{Dir}/Backups/{module}", true);
      var pdb = Path.ChangeExtension (module, ".pdb");
      File.Copy ($"{Dir}/{pdb}", $"{Dir}/Backups/{pdb}", true);
   }

   // Disassemble the DLL to create IL assembly files
   void Disassemble (string module) {
      Console.WriteLine ($"Disassembling {module}");
      var ildasmNew = $"{Dir}/ASMCore/ildasm.exe";
      var ildasmOld = $"{Dir}/ASMFramework/ildasm.exe";
      ExecProgram (ildasmOld, $"/LINENUM /TOKENS /out={Dir}/lines.asm {Dir}/{module}");
      ExecProgram (ildasmNew, $"/TOKENS /out={Dir}/nolines.asm {Dir}/{module}");

      string[] text1 = File.ReadAllLines ($"{Dir}/lines.asm").Where (a => !string.IsNullOrWhiteSpace (a)).ToArray ();
      List<string> text2 = File.ReadAllLines ($"{Dir}/nolines.asm").Where (a => !string.IsNullOrWhiteSpace (a)).ToList ();
      int n2 = 0;
      for (int n1 = 0; n1 < text1.Length; n1++) {
         var line = text1[n1].Trim ();
         if (line.StartsWith (".method /*")) {
            // Sync pointer n2 to the same method in the nolines.asm text
            for (; ; n2++)
               if (text2[n2] == text1[n1]) break;
            n2++;
            continue;
         }
         if (line.StartsWith (".line") && !line.StartsWith (".line 16707566")) {
            var match = rIL.Match (text1[n1 + 1].Trim ());
            if (match.Success) {
               SeekTo (match.Value);
               text2.Insert (n2, text1[n1]); n2++;
               continue;
            }
            match = rIL.Match (text1[n1 - 1].Trim ());
            if (match.Success) {
               SeekTo (match.Value);
               text2.Insert (n2 + 1, text1[n1]); n2++;
               continue;
            }
            throw new Exception ($"Could not match {line}");
         }
      }
      var asmFile = Path.ChangeExtension (module, ".original.asm");
      File.WriteAllLines ($"{Dir}/{asmFile}", text2.ToArray ());
      File.Delete ($"{Dir}/lines.asm");
      File.Delete ($"{Dir}/nolines.asm");

      // Helper .................................
      void SeekTo (string label) {
         for (; ; n2++) {
            var line2 = text2[n2].Trim ();
            if (line2.StartsWith (".method /*")) throw new Exception ("Found next method");
            var match1 = rIL.Match (line2);
            if (match1.Value == label) return;
         }
      }
   }
   static Regex rIL = new (@"^IL_[0-9a-fA-F][0-9a-fA-F][0-9a-fA-F][0-9a-fA-F]:", RegexOptions.Compiled);

   // Add the instrumentation (add a hit after each .line)
   void AddInstrumentation (string module) {
      module = Path.GetFileNameWithoutExtension (module);
      var infile = $"{Dir}/{module}.original.asm";
      var outfile = $"{Dir}/{module}.asm";
      string[] input = File.ReadAllLines (infile).Select (ModifyJumps).ToArray ();
      List<string> output = new ();
      for (int i = 0; i < input.Length; i++) {
         var s1 = input[i];
         output.Add (s1);
         if (s1.Trim ().StartsWith (".line ")) {
            var match = mRxLine.Match (s1);
            if (!match.Success) throw new Exception ("Unexpected .line directive");
            var s2 = input[i + 1];
            int colon = s2.IndexOf (':') + 1;
            if (colon != 0) {
               var groups = match.Groups;
               int nBlock = mBlocks.Count;
               mBlocks.Add (new Block (nBlock, int.Parse (groups[1].Value), int.Parse (groups[2].Value),
                  int.Parse (groups[3].Value), int.Parse (groups[4].Value), groups[5].Value));

               var label = s2[..colon];
               output.Add ($"{label} ldc.i4 {nBlock}");
               output.Add ("             call void [CoverLib]CoverLib.HitCounter::Hit(int32)");
               output.Add ("           " + s2[colon..]);
               i++;
            }
         }
      }
      File.WriteAllLines (outfile, output);
   }
   static string ModifyJumps (string s) {
      if (!s.Contains (".s ")) return s;
      foreach (var jump in sJumps)
         s = s.Replace ($" {jump}.s ", $" {jump} ");
      return s;
   }
   static string[] sJumps = new[] {
      "leave", "br", "beq", "bge", "bge.un", "bgt", "bgt.un",
      "ble", "ble.un", "blt", "blt.un", "bne", "bne.un",
      "brfalse", "brnull", "brzero", "brtrue", "brinst"
   };
   static Regex mRxLine = new Regex (@"\.line (\d+),(\d+) : (\d+),(\d+) '(.*)'");
   List<Block> mBlocks = new ();

   // Re-assemble instrumented DLLs from the modified ASMs
   void Assemble (string module) {
      Console.WriteLine ($"Assembling {module}");
      File.Delete ($"{Dir}/{module}");
      var ilasm = $"{Dir}/ASMCore/ilasm.exe";
      var asmfile = $"{Dir}/{Path.GetFileNameWithoutExtension (module)}.asm";
      ExecProgram (ilasm, $"/QUIET /dll /PE64 /X64 {asmfile} /output={Dir}/{module}");
   }

   // Run the instrumented program to gather data (hits)
   void RunCode () {
      Console.WriteLine ("Running program");
      ExecProgram ($"{Dir}/{RunExe}", "");
   }

   // Generate output HTML (colored source code with hit / unhit areas marked)
   void GenerateOutputs () {
      // To colour-code and add Tool-tip to the code-----------------------------------------------
      ulong[] hits = File.ReadAllLines ($"{Dir}/hits.txt").Select (ulong.Parse).ToArray ();
      var files = mBlocks.Select (a => a.File).Distinct ().ToArray ();
      foreach (var file in files) {
         var blocks = mBlocks.Where (a => a.File == file)
                             .OrderBy (a => a.SPosition)
                             .ThenByDescending (a => a.EPosition)
                             .ToList ();
         for (int i = blocks.Count - 1; i > 0; i--)
            if (blocks[i - 1].Contains (blocks[i]))
               blocks.RemoveAt (i - 1);
         blocks.Reverse ();

         var code = File.ReadAllLines (file);
         for (int i = 0; i < code.Length; i++)
            code[i] = code[i].Replace ('<', '\u00ab').Replace ('>', '\u00bb');
         foreach (var block in blocks) {
            ulong bID = hits[block.Id];
            bool hit = hits[block.Id] > 0;
            string tag = $"<span class=" + (hit ? $"\"hit\" title= \"No.of hits = {bID}\"" : "\"unhit\"") + ">";
            for (int l = block.SLine; l <= block.ELine; l++) {
               var iP = (l == block.SLine) ? block.SCol : code[l].TakeWhile (char.IsWhiteSpace).Count ();
               var fP = (l == block.ELine) ? block.ECol : code[l].Length;
               code[l] = code[l].Insert (fP, "</span>");
               code[l] = code[l].Insert (iP, tag);
            }
         }

         Directory.CreateDirectory ("HTML");
         string htmlfile1 = $"{Dir}/HTML/{Path.GetFileNameWithoutExtension (file)}.html";

         string html1 = $$"""
            <html><head><style>

            .hit { background-color:aqua; }
            .unhit { background-color:orange; }
            </style></head>

            <body><pre>
            {{string.Join ("\r\n", code)}}
            </pre></body></html>
            """;
         html1 = html1.Replace ("\u00ab", "&lt;").Replace ("\u00bb", "&gt;");
         File.WriteAllText (htmlfile1, html1);
      }

      // To Generate Summary Table-----------------------------------------------------------------
      List<Tuple<string, int, int, double>> Contents = new ();
      string lines = "";
      foreach (var file in files) {
         var fBlocks = mBlocks.Where (a => a.File == file);
         var fBlocksCount = fBlocks.Count ();
         var cBlocksCount = fBlocks.Where (a => hits[a.Id] > 0).Count ();
         double coverage = Math.Round (100.0 * cBlocksCount / fBlocksCount, 1);
         string path = Directory.GetCurrentDirectory ()[0..^3];
         Contents.Add (Tuple.Create (Path.GetFileName (file), fBlocksCount, cBlocksCount, coverage));
      }
      Contents = Contents.OrderBy (i => i.Item4).ToList ();
      foreach (var v in Contents) {
         lines += $$"""
            <tr>
               <td>{{v.Item1}}</td>
               <td>{{v.Item2}}</td>
               <td>{{v.Item3}}</td>
               <td>{{v.Item4}}%</td>
            </tr>
            """;
      }
      string htmlfile2 = $"{Dir}/HTML/Summary.html";
      string html2 = $$"""
         <html><head><style>
         table {
            font-family: arial, sans-serif;
            border-collapse: collapse;
            width: 100%
         }

         td, th {
            border: 1px outset black;
            text-align: center;
            padding: 8x;
         }

         td:nth-child(even), th:nth-child(even) {
           background-color: #D6EEEE;
         }
         </style></head>

         <body>
         <h2>Summay Table</h2>
         <table>
            <tr>
               <th>Source Code</th>
               <th>Total Blocks</th>
               <th>Covered Blocks</th>
               <th>Code Coverage</th>
            </tr>
         {{lines}}
         </table></body></html>
         """;
      html2 = html2.Replace ("\u00ab", "&lt;").Replace ("\u00bb", "&gt;");
      File.WriteAllText (htmlfile2, html2);

      int cBlocks = mBlocks.Count, cHit = hits.Count (a => a > 0);
      double percent = Math.Round (100.0 * cHit / cBlocks, 1);
      Console.WriteLine ($"Coverage: {cHit}/{cBlocks}, {percent}%");
   }

   void RestoreBackup (string module) {
      Console.WriteLine ("Restoring backups");
      Directory.CreateDirectory ($"{Dir}/Backups");
      File.Copy ($"{Dir}/Backups/{module}", $"{Dir}/{module}", true);
      var pdb = Path.ChangeExtension (module, ".pdb");
      File.Copy ($"{Dir}/Backups/{pdb}", $"{Dir}/{pdb}", true);
   }

   // Execute an external program, and wait for it to complete
   // (Also throws an exception if the external program returns a non-zero error code)
   static void ExecProgram (string name, string args) {
      var proc = Process.Start (name, args);
      proc.WaitForExit ();
      if (proc.ExitCode != 0)
         throw new Exception ($"Process {name} returned code {proc.ExitCode}");
   }
}

// Represents a basic code-coverage block (contiguous block of C# code)
class Block {
   public Block (int id, int sLine, int eLine, int sCol, int eCol, string file) {
      if (file == "") file = sLastFile;
      (Id, SLine, ELine, SCol, ECol, File) = (id, sLine - 1, eLine - 1, sCol - 1, eCol - 1, file);
      sLastFile = file;
   }

   public bool Contains (Block c) {
      if (File != c.File) return false;
      if (c.SPosition < SPosition) return false;
      if (c.EPosition > EPosition) return false;
      return true;
   }

   public override string ToString ()
      => $"{SLine},{ELine} : {SCol},{ECol} of {File}";

   public readonly int Id;
   public readonly int SLine, ELine, SCol, ECol;
   public int SPosition => SLine * 10000 + SCol;
   public int EPosition => ELine * 10000 + ECol;
   public readonly string File;
   static string sLastFile = "";
}

static class Program {
   public static void Main () {
      var analyzer = new Analyzer ("P:/Bin", "PSITest.exe", "parser.dll");
      analyzer.Run ();
   }
}
