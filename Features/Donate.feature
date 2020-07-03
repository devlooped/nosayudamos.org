#language: es-ES
Característica: Donacion

Antecedentes:
    Dado Un donante cualquiera

@Draft
Escenario: Primer donacion
    Cuando Envia 'Donar $500'
    Entonces Recibe 'Gracias!'
