; format PE console
; entry main

; include '%SALO_include%\MACRO\import32.inc'

; section '.data' data readable writeable
; emg db 'error',0dh,0ah,0
; msg db 'привіт світ!',0dh,0ah,0
; lcl_set db 0
; lcl_bi dd 0
; lcl_bo dd 0

; section '.code' code readable executable

; main:
; ;fail without set locale
; push	msg
; call	[printf]
 ; pop	ecx

; ;succeed with set locale
; push	msg
; call	_liapnuty
 ; pop	ecx
 
; call	__reset_locale
 
; push	0
; call	[ExitProcess]


;Error with application termination
__err:
push	emsg
call	[printf]
 pop	ecx
 mov	eax, 0
push	0
call	[ExitProcess]


;Ukrainian print message
_liapnuty:
push	ebp
 mov	ebp, esp
;sub	esp, 0
 mov	ebx,[ebp+8]  ; 1st arg addr
 
 mov	al, [lcl_set]
  or	al, al
 jnz	_liapnuty_rest
call	__set_locale
 
_liapnuty_rest:
push	ebx
call	[printf]
 pop	ebx
 
 mov	esp, ebp
 pop	ebp
 retn
 
 
;Set console locale to Ukrainian
__set_locale:
 mov	al, [lcl_set]
  or	al, al
 jnz	__set_locale_rest
 
call	[GetConsoleCP]
 mov	[lcl_bi],eax
call	[GetConsoleOutputCP]
 mov	[lcl_bo],eax
 
push	1251
call	[SetConsoleCP]
  or	eax, eax
  jz	__err

push	1251
call	[SetConsoleOutputCP]
  or	eax, eax
  jz	__err
  
 mov	[lcl_set],1

__set_locale_rest:
 xor	eax,eax
 mov	al,[lcl_set]
retn


;Reset console locale after usage
__reset_locale:
 mov	al, [lcl_set]
  or	al, al
  jz	__reset_locale_rest
  
push	[lcl_bi]
call	[SetConsoleCP]
  or	eax, eax
  jz	__err

push	[lcl_bo]
call	[SetConsoleOutputCP]
  or	eax, eax
  jz	__err
  
 mov	[lcl_set],0

__reset_locale_rest:
 xor	eax,eax
 mov	al,[lcl_set]
retn
