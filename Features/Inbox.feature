#language: es-ES
Característica: Bandeja de entrada
	Para registrar a nuevos usuarios
    Implementamos una bandeja de entrada
    Que determina qué tipo de usuarios registrar

Antecedentes:
    Dado Un usuario no registrado

Escenario: Contacto inicial de un donatario
	Cuando Envia '<mensaje>'
	Entonces Recibe 'UI_Donee_SendIdentifier'
		"""
		Gracias por tu mensaje! Por favor envianos una 
		foto de tu DNI para registrarte para recibir ayuda.
		"""

	Ejemplos:
		| mensaje         |
		| Necesito ayuda  |
		| Ayuda           |
		| Dame plata      |
		| Prestame dinero |

Escenario: Contacto inicial con mensaje confuso
	Cuando Envia mensaje
		"""
		Foo
		"""
	Entonces Recibe 'UI_UnknownIntent'
		"""
		Gracias por tu mensaje! Comentanos si necesitas 
		ayuda o estás interesando en ayudar a otros.
		"""

Escenario: Contacto inicial de un donante
	Cuando Envia mensaje
		"""
		Quiero ayudar
		"""
	Entonces Recibe 'UI_Donor_SendAmount'
		"""
		Gracias por tu mensaje! Cuánto querés donar?
		"""
