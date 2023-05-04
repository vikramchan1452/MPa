program Expr;
var
  i, fib: integer;

function Fibo (n: integer) : integer;
var 
  i, prod: integer;
begin 
  prod := 1;
  for i := 1 to n do begin
    prod := prod * i;
  end;
  Fibo := prod;
end;

begin
  for i := 1 to 10 do begin
    fib := Fibo (i);
    WriteLn ("Fibo(", i, ") = ", fib);
  end;
end.
