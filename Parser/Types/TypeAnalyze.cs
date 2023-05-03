// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// TypeAnalyze.cs ~ Type checking, type coercion
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;

using System.Linq;
using System.Net.Mail;
using static NType;
using static Token.E;

//Same name used for a function and a variable, or a function and a const, or a const and a variable within a block - error ***
//Correct interpretation of same variable name used in an inner block with a different type ***
//Adding NTypeCast nodes before a function is called, on the parameters to the function ***

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
      Visit (d.Vars); return Visit (d.Funcs);
   }

   public override NType Visit (NConstDecl c) {
      if (mSymbols.Consts.Contains(c)) throw new ParseException (c.Name, "Constant name already defined");
      if (mSymbols.Find (c.Name.Text) is NVarDecl) throw new ParseException (c.Name, "Constant name already defined as variable");
      if (mSymbols.Find (c.Name.Text) is NFnDecl) throw new ParseException (c.Name, "Constant name already defined as function");
      mSymbols.Consts.Add (c);
      return Void;
   }

   public override NType Visit (NVarDecl d) {
      if (mSymbols.Vars.Contains (d)) throw new ParseException (d.Name, "Variable name already defined");
      //if (mSymbols.Find (d.Name.Text) is NVarDecl v && d.Type == v.Type) throw new ParseException (d.Name, "Variable name already defined with same type");
      //if (mSymbols.Find (d.Name.Text) is NConstDecl) throw new ParseException (d.Name, "Variable name already defined as constant");
      //if (mSymbols.Find (d.Name.Text) is NFnDecl) throw new ParseException (d.Name, "Variable name already defined as function");
      mSymbols.Vars.Add (d);
      return d.Type;
   }

   public override NType Visit (NFnDecl f) {
      if (mSymbols.Funcs.Contains (f)) throw new ParseException (f.Name, "Function name already defined");
      if (mSymbols.Find (f.Name.Text) is NConstDecl) throw new ParseException (f.Name, "Function name already defined as constant");
      if (mSymbols.Find (f.Name.Text) is NVarDecl) throw new ParseException (f.Name, "Function name already defined as variable");
      mSymbols = new SymTable { Parent = mSymbols };
      mSymbols.Vars.Add (new NVarDecl (f.Name, f.Return));
      if (mSymbols.Find (f.Name.Text) is not NFnDecl) {
         Visit (f.Params);
         f.Body?.Accept(this);
         mSymbols = mSymbols.Parent;
         return f.Return;
      }
      throw new ParseException (f.Name, "Unknown function");
   }
   #endregion

   #region Statements --------------------------------------
   public override NType Visit (NCompoundStmt b)
      => Visit (b.Stmts);

   public override NType Visit (NAssignStmt a) {
      if (mSymbols.Find (a.Name.Text) is not NVarDecl v)
         throw new ParseException (a.Name, "Unknown variable");
      a.Expr.Accept (this);
      a.Expr = AddTypeCast (a.Name, a.Expr, v.Type);
      return v.Type;
   }
   
   NExpr AddTypeCast (Token token, NExpr source, NType target) {
      if (source.Type == target) return source;
      bool valid = (source.Type, target) switch {
         (Int, Real) or (Char, Int) or (Char, String) => true,
         _ => false
      };
      if (!valid) throw new ParseException (token, "Invalid type");
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

   public override NType Visit (NReadStmt r) 
      => Void; 

   public override NType Visit (NWhileStmt w) {
      w.Condition.Accept (this); w.Body.Accept (this);
      return Void; 
   }

   public override NType Visit (NRepeatStmt r) {
      Visit (r.Stmts); r.Condition.Accept (this);
      return Void;
   }
    
   public override NType Visit (NCallStmt c) {
      if (mSymbols.Find (c.Name.Text) is NFnDecl)
         return Visit (c.Params);
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
      if (mSymbols.Find (d.Name.Text) is not NConstDecl c)
         throw new ParseException (d.Name, "Unknown variable");
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
