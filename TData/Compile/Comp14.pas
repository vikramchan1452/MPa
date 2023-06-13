program Comp14;
var
   i, k, limit : integer;
   cond : boolean;

begin
	k := 0;
	cond := true;
	limit := 10000;
	while limit > 9999 do begin
		write("Please enter a 4-digit number: ");
		readln(limit);
	end;
	writeln("\nSQUARES UPTO ", limit, ":");
	for i := 1 to 50 do begin
		if i * i < limit then begin
			if i mod 10 = 1 then
				write(i - 1, "s --> ");
		end;
		repeat
			if i * i > limit then
				break 2;
			write(i * i, ", ");
			k := k + 1;
			if i * i mod 10 = 0 then 
				writeln();
			break;
		until cond = true;
	end;
	
	for i := 51 to 100 do begin
		if i * i < limit then begin
			if i mod 10 = 1 then
				write(i - 1, "s --> ");
		end;
		while cond = true do begin
			if i * i > limit then
				break 2;
			write(i * i, ", ");
			k := k + 1;
			if i * i mod 10 = 0 then 
				writeln();
			break 1;
		end;
	end;
	writeln("\nThere are ", k, " squares till ", limit, ".");
end.