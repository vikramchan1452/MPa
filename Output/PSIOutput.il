.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 7:0:0:0 }
.assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 7:0:0:0 }
.assembly extern PSILib { .ver 1:0:0:0 }
.assembly Comp4 { .ver 0:0:0:0 }

.class Program {
    .field static int32 i
    .field static int32 j
  .method static void Main () {
    .entrypoint
  IL_0001:
    ldc.i4 2
    ldc.i4 3
    clt
    brfalse IL_0002
    ldstr "2 < 3"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0002: nop
  IL_0003:
    ldc.i4 3
    ldc.i4 4
    leq
    brfalse IL_0004
    ldstr "3 <= 4"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0004: nop
  IL_0005:
    ldc.i4 2
    ldc.i4 2
    ceq
    brfalse IL_0006
    ldstr "2 = 2"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0006: nop
  IL_0007:
    ldc.i4 2
    ldc.i4 3
    neq
    brfalse IL_0008
    ldstr "2 <> 3"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0008: nop
  IL_0009:
    ldc.i4 4
    ldc.i4 3
    gt
    brfalse IL_0010
    ldstr "4 > 3"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0010: nop
  IL_0011:
    ldc.i4 4
    ldc.i4 3
    geq
    brfalse IL_0012
    ldstr "4 >= 3"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0012: nop
  IL_0013:
    ldc.i4 3
    ldc.i4 2
    clt
    brfalse IL_0014
    ldstr "3 < 2 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0014: nop
  IL_0015:
    ldc.i4 4
    ldc.i4 3
    leq
    brfalse IL_0016
    ldstr "4 <= 3 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0016: nop
  IL_0017:
    ldc.i4 2
    ldc.i4 3
    ceq
    brfalse IL_0018
    ldstr "2 = 3 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0018: nop
  IL_0019:
    ldc.i4 2
    ldc.i4 2
    neq
    brfalse IL_0020
    ldstr "2 <> 2 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0020: nop
  IL_0021:
    ldc.i4 3
    ldc.i4 4
    gt
    brfalse IL_0022
    ldstr "3 > 4 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0022: nop
  IL_0023:
    ldc.i4 3
    ldc.i4 4
    geq
    brfalse IL_0024
    ldstr "3 >= 4 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0024: nop
  IL_0025:
    ldc.i4 2
    ldc.i4 3
    clt
    ldc.i4 3
    ldc.i4 4
    clt
    and
    brfalse IL_0026
    ldstr "2 < 3 and 3 < 4"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0026: nop
  IL_0027:
    ldc.i4 2
    ldc.i4 3
    clt
    ldc.i4 4
    ldc.i4 3
    clt
    and
    brfalse IL_0028
    ldstr "2 < 3 and 4 < 3 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0028: nop
  IL_0029:
    ldc.i4 2
    ldc.i4 3
    clt
    ldc.i4 4
    ldc.i4 3
    clt
    or
    brfalse IL_0030
    ldstr "2 < 3 or 4 < 3"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0030: nop
  IL_0031:
    ldc.i4 3
    ldc.i4 2
    clt
    ldc.i4 4
    ldc.i4 3
    clt
    or
    brfalse IL_0032
    ldstr "3 < 2 or 4 < 3 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0032: nop
  IL_0033:
    ldc.i4 3
    ldc.i4 2
    clt
    not
    brfalse IL_0034
    ldstr "not 3 < 2"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0034: nop
  IL_0035:
    ldc.i4 2
    ldc.i4 3
    clt
    not
    brfalse IL_0036
    ldstr "not 2 < 3 ???"
    call void [System.Console]System.Console::Write (string)
    call void [System.Console]System.Console::WriteLine ()
  IL_0036: nop
    ret
  }
}
