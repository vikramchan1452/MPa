// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// PSIPrint.cs ~ Prints a PSI syntax tree in Pascal format
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.Diagnostics;

namespace PSI;

public class PSIPrint : Visitor<StringBuilder> {
   public override StringBuilder Visit (NProgram p) {
      Write ($"program {p.Name}; ");
      Visit (p.Block);
      return Write (".");
   }

   public override StringBuilder Visit (NBlock b)
      => Visit (b.Decls, b.Body);

   public override StringBuilder Visit (NDeclarations d) {
      if (d.Vars.Length > 0) {
         NWrite ("var"); N++;
         foreach (var g in d.Vars.GroupBy (a => a.Type))
            NWrite ($"{g.Select (a => a.Name).ToCSV ()} : {g.Key};");
         N--;
      }
      return S;
   }

   public override StringBuilder Visit (NVarDecl d)
      => NWrite ($"{d.Name} : {d.Type}");

   public override StringBuilder Visit (NProcDecl d) {
      Write ($"procedure {d.Name} (");
      for (int i = 0; i < d.Vars.Length; i++) {
         if (i > 0) Write (", ");
         d.Vars[i].Accept (this);
      }
      Write (")");

      Visit (d.Block); Write (";");
      return S;
   }

   public override StringBuilder Visit (NFuncDecl d) {
      Write ($"function {d.Name} (");
      for (int i = 0; i < d.Vars.Length; i++) {
         if (i > 0) Write (", ");
         d.Vars[i].Accept (this);
      }
      Write ($") : {d.Type}");
      Visit (d.Block); Write (";");
      return S;
   }

   public override StringBuilder Visit (NCompoundStmt b) {
      NWrite ("begin"); N++; Visit (b.Stmts); N--; return NWrite ("end;");
   }
   public override StringBuilder Visit (NReadStmt r)
     => NWrite ($"read ({r.Name});");

   public override StringBuilder Visit (NAssignStmt a) {
      NWrite ($"{a.Name} := "); a.Expr.Accept (this); return Write (";");
   }

   public override StringBuilder Visit (NWriteStmt w) {
      NWrite (w.NewLine ? "WriteLn (" : "Write (");
      for (int i = 0; i < w.Exprs.Length; i++) {
         if (i > 0) Write (", ");
         w.Exprs[i].Accept (this);
      }
      return NWrite (");");
   }

   public override StringBuilder Visit (NCallStmt c) {
      Write ($"{c.Name} "); Visit (c.Exprs);
      return S;
   }

   public override StringBuilder Visit (NIfStmt i) {
      Write ("if "); N++; i.Expr.Accept (this); Write (" then"); Visit (i.Stmts); N--;
      NWrite ("else"); N++; Visit (i.Stmts); N--;
      return S;
   }

   public override StringBuilder Visit (NWhileStmt w) {
      Write ("while "); N++; w.Expr.Accept (this);
      Write (" do"); Visit (w.Stmt); N--;
      return NWrite ("end;");
   }

   public override StringBuilder Visit (NRepeatStmt r) {
      Write ("repeat "); N++;
      for (int i = 0; i < r.Stmts.Length; i++) {
         if (i > 0) Write ("; ");
         Visit (r.Stmts);
      }
      N--;
      NWrite ("until "); r.Expr.Accept (this);
      return S;
   }

   public override StringBuilder Visit (NForStmt f) {
      Write ("for "); N++;
      Write ($"{f.Name} := "); Visit (f.Exprs); Write (f.IsTo ? "to " : "downto ");
      Visit (f.Exprs);
      Write ("do "); NWrite (""); Visit (f.Exprs); N--;
      return S;
   }

   public override StringBuilder Visit (NLiteral t)
      => Write (t.Value.ToString ());

   public override StringBuilder Visit (NIdentifier d)
      => Write (d.Name.Text);

   public override StringBuilder Visit (NUnary u) {
      Write (u.Op.Text); return u.Expr.Accept (this);
   }

   public override StringBuilder Visit (NBinary b) {
      Write ("("); b.Left.Accept (this); Write ($" {b.Op.Text} ");
      b.Right.Accept (this); return Write (")");
   }

   public override StringBuilder Visit (NFnCall f) {
      Write ($"{f.Name} (");
      for (int i = 0; i < f.Params.Length; i++) {
         if (i > 0) Write (", "); f.Params[i].Accept (this);
      }
      return Write (")");
   }

   StringBuilder Visit (params Node[] nodes) {
      nodes.ForEach (a => a.Accept (this));
      return S;
   }

   // Writes in a new line
   StringBuilder NWrite (string txt)
      => Write ($"\n{new string (' ', N * 3)}{txt}");
   int N;   // Indent level

   // Continue writing on the same line
   StringBuilder Write (string txt) {
      Console.Write (txt);
      S.Append (txt);
      return S;
   }

   public override StringBuilder Visit (NArgList l) {
      throw new NotImplementedException ();
   }


   readonly StringBuilder S = new ();
}