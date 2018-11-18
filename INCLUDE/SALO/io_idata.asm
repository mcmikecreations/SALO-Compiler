;section '.idata' import data readable
library	kernel,'kernel32.dll',\
		msvcrt,'msvcrt.dll'
	
import	kernel,\
		GetConsoleCP,'GetConsoleCP',\
		GetConsoleOutputCP,'GetConsoleOutputCP',\
		SetConsoleCP,'SetConsoleCP',\
		SetConsoleOutputCP,'SetConsoleOutputCP',\
		ExitProcess,'ExitProcess'

import	msvcrt,\
		printf,'printf',\
		setlocale,'setlocale'