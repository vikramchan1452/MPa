program Comp13;
var
    score: integer;
    s: string;
    c: char;
    i: integer;
    r: real;
    b: boolean;
    
begin
  write("Enter your Name: ");
  readln (s);
  writeln("Hi ", s, "! Answer the questions:- ");
  write("1) 5 + 2 = ");
  readln (i);
  write("2) 5 ÷ 2 = ");
  readln (r);
  write("3) (5 > 2)? [true/false]: ");
  readln (b);
  write("4) Is 57 a Prime Number? [y/n]: ");
  readln (c);
  
  if (i = 7) then score := score + 1;
  if (r = 2.5) then score := score + 1;
  if (b = true) then score := score + 1;
  if (c = 'n') then score := score + 1;
  writeln();
  writeln( "Score: ", score*25, " out of 100.");

end.