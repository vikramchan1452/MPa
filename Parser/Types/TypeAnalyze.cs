// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// TypeAnalyze.cs ~ Type checking, type coercion
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;

using System.Linq;
using System.Xml.Linq;
using static NType;
using static Token.E;

//Correct interpretation of same variable name used in an inner block with a different type ***
//Adding NTypeCast nodes before a function is called, on the parameters to the function 
//Checking a variable is assigned before it is actually read in a block

public class TypeAnalyze : Visitor<NType> {  
   public TypeAnalyze () {
      mSymbols = SymTable.Root;
   }
   SymTable mSymbols;

   #region Declarations ------------------------------------
   public override NType Visit (NProgram p) 
      => Visit (p.Block);
   
   public override NType Visit (NBlock b) {
      mSymbols = new SymTable { Parent = mSymbols };
      Visit (b.Declarations); Visit (b.Body);
      mSymbols = mSymbols.Parent;
      return Void;
   }

   public override NType Visit (NDeclarations d) {
      Visit (d.Consts); Visit (d.Vars); return Visit (d.Funcs);          
   }

   public override NType Visit (NConstDecl c) {
      if (mSymbols.Consts.Contains(c)) throw new ParseException (c.Name, "Constant name has already defined in the same block");
      if (mSymbols.Find (c.Name.Text) is NVarDecl) throw new ParseException (c.Name, "Constant name has already defined as variable in the same block");
      if (mSymbols.Find (c.Name.Text) is NFnDecl) throw new ParseException (c.Name, "Constant name has already defined as function in the same block");
      Visit (c.Value); mSymbols.Consts.Add (c); 
      return c.Value.Type;
   }

   public override NType Visit (NVarDecl d) {
      if (mSymbols.Vars.Contains (d)) throw new ParseException (d.Name, "Variable name has already defined in the same block");
      if (mSymbols.Find (d.Name.Text) is NVarDecl && mSymbols.Parent?.Find (d.Name.Text) is NVarDecl v && d.Type == v.Type)
         throw new ParseException (d.Name, "Variable name has already defined with same type in the outer block");
      if (mSymbols.Find (d.Name.Text) is NConstDecl) throw new ParseException (d.Name, "Variable name has already defined as constant in the same block");
      if (mSymbols.Find (d.Name.Text) is NFnDecl) throw new ParseException (d.Name, "Variable name has already defined as function in the same block");
      mSymbols.Vars.Add (d);
      return d.Type;
   }

   public override NType Visit (NFnDecl f) {
      if (mSymbols.Funcs.Contains (f)) throw new ParseException (f.Name, "Function name has already defined in the same block");
      if (mSymbols.Find (f.Name.Text) is NConstDecl) throw new ParseException (f.Name, "Function name has already defined as constant in the same block");
      if (mSymbols.Find (f.Name.Text) is NVarDecl) throw new ParseException (f.Name, "Function name has already defined as variable in the same block");
      mSymbols.Funcs.Add (f);
      Visit (f.Params);
      mSymbols = new SymTable { Parent = mSymbols };
      foreach (var v in f.Params) v.Assigned = true;
      mSymbols.Vars.Add (new NVarDecl (f.Name, f.Return));
      if (f.Body != null) {
         f.Body?.Accept(this);
         if (!f.Assigned && f.Return != Void) throw new ParseException (f.Name, "Function return value is not set");
      }
      mSymbols = mSymbols.Parent;
      return f.Return;
   }
   #endregion

   #region Statements --------------------------------------
   public override NType Visit (NCompoundStmt b)
      => Visit (b.Stmts);

   public override NType Visit (NAssignStmt a) {
      a.Expr.Accept (this);
      NType type;
      switch (mSymbols.Find (a.Name.Text)) {
         case NVarDecl v: type = v.Type; v.Assigned = true; break;
         case NFnDecl f: type = f.Return; f.Assigned = true; break;
         case NConstDecl: throw new ParseException (a.Name, "Constant cannot be assigned");
         default: throw new ParseException (a.Name, "Unknown variable");
      }
      a.Expr = AddTypeCast (a.Name, a.Expr, type);
      return type;
   }
    
   NExpr AddTypeCast (Token token, NExpr source, NType target) {
      if (source.Type == target) return source;
      bool valid = (source.Type, target) switch {
         (Int, Real) or (Char, Int) or (Char, String) => true,
         _ => false
      };
      if (!valid) throw new ParseException (token, $"Expecting {target}, but found{source}");
      return new NTypeCast (source) { Type = target };
   }

   public override NType Visit (NWriteStmt w)
      => Visit (w.Exprs);

   public override NType Visit (NIfStmt f) {
      f.Condition.Accept (this);
      f.IfPart.Accept (this); f.ElsePart?.Accept (this);
      return Void;
   }

   public override NType Visit (NForStmt f) {
      f.Start.Accept (this); f.End.Accept (this); f.Body.Accept (this);
      return Void;
   }

   public override NType Visit (NReadStmt r) {
      foreach (var a in r.Vars) {
         switch (mSymbols.Find (a.Text)) {
            case NVarDecl v: v.Assigned = true; return v.Type;
            case NConstDecl: throw new ParseException (a, "Expecting variable, but found constantnown variable");
            default: throw new ParseException (a, "Unknown variable");
         }
      }
      return Void;
   }

   public override NType Visit (NWhileStmt w) {
      w.Condition.Accept (this); w.Body.Accept (this);
      return Void; 
   }

   public override NType Visit (NRepeatStmt r) {
      Visit (r.Stmts); r.Condition.Accept (this);
      return Void;
   }
    
   public override NType Visit (NCallStmt c) {
      if (mSymbols.Find (c.Name.Text) is NFnDecl g) {
         if (c.Params.Length != g.Params.Length) throw new ParseException (c.Name, $"Parameters count is mismatching. '{g.Name}' procedure requires {g.Params.Length} parameters.");
         Visit (c.Params);
         for (int i = 0; i < g.Params.Length; i++) {
            if (c.Params[i].Type != g.Params[i].Type) throw new ParseException (c.Name, $"Parameter type doesn't match. Parameter-{i + 1}: Expecting {g.Params[i].Type}, but found{c.Params[i].Type}");
         }
         return g.Return;
      }
      throw new ParseException (c.Name, "Unknown procedure");
   }
   #endregion

   #region Expression --------------------------------------
   public override NType Visit (NLiteral t) {
      t.Type = t.Value.Kind switch {
         L_INTEGER => Int, L_REAL => Real, L_BOOLEAN => Bool, L_STRING => String,
         L_CHAR => Char, _ => Error,
      };
      return t.Type;
   }

   public override NType Visit (NUnary u) 
      => u.Expr.Accept (this);

   public override NType Visit (NBinary bin) {
      NType a = bin.Left.Accept (this), b = bin.Right.Accept (this);
      bin.Type = (bin.Op.Kind, a, b) switch {
         (ADD or SUB or MUL or DIV, Int or Real, Int or Real) when a == b => a,
         (ADD or SUB or MUL or DIV, Int or Real, Int or Real) => Real,
         (MOD, Int, Int) => Int,
         (ADD, String, _) => String, 
         (ADD, _, String) => String,
         (LT or LEQ or GT or GEQ, Int or Real, Int or Real) => Bool,
         (LT or LEQ or GT or GEQ, Int or Real or String or Char, Int or Real or String or Char) when a == b => Bool,
         (EQ or NEQ, _, _) when a == b => Bool,
         (EQ or NEQ, Int or Real, Int or Real) => Bool,
         (AND or OR, Int or Bool, Int or Bool) when a == b => a,
         _ => Error,
      };
      if (bin.Type == Error)
         throw new ParseException (bin.Op, "Invalid operands");
      var (acast, bcast) = (bin.Op.Kind, a, b) switch {
         (_, Int, Real) => (Real, Void),
         (_, Real, Int) => (Void, Real), 
         (_, String, not String) => (Void, String),
         (_, not String, String) => (String, Void),
         _ => (Void, Void)
      };
      if (acast != Void) bin.Left = new NTypeCast (bin.Left) { Type = acast };
      if (bcast != Void) bin.Right = new NTypeCast (bin.Right) { Type = bcast };
      return bin.Type;
   }

   public override NType Visit (NIdentifier d) {
      if (mSymbols.Find (d.Name.Text) is NVarDecl v )
         return d.Type = v.Type;
      if (mSymbols.Find (d.Name.Text) is NConstDecl c)
         return d.Type = c.Value.Type;
      return Void;
   }

   public override NType Visit (NFnCall f) {
      if (mSymbols.Find (f.Name.Text) is NFnDecl g) {
         if (f.Params.Length != g.Params.Length) throw new ParseException (f.Name, $"Parameters count is mismatching. '{g.Name}' function requires {g.Params.Length} parameters.");
         Visit (f.Params);
         for (int i = 0; i < g.Params.Length; i++) {
         f.Params[i] = AddTypeCast (f.Name, f.Params[i], f.Type);
         if (f.Params[i].Type != g.Params[i].Type) throw new ParseException (f.Name, $"Parameter type doesn't match. Parameter No.{i + 1} should be a {g.Params[i].Type}, but {f.Params[i].Type} here.");
         }
         return f.Type = g.Return;
      }
      throw new ParseException (f.Name, "Unknown function");
   }

   public override NType Visit (NTypeCast c) {
      c.Expr.Accept (this); return c.Type;
   }
   #endregion

   NType Visit (IEnumerable<Node> nodes) {
      foreach (var node in nodes) node.Accept (this);
      return Void;
   }
}
