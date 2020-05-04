#language: es-ES
Característica: Donacion

Antecedentes:
    Dado Un storage limpio
	Y un donante cualquiera

@Draft
Escenario: Primer donacion
    Cuando Envia 'Donar $500'
    Entonces Recibe 'Gracias!'
