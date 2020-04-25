#language: es-ES
Característica: Bandeja de entrada
	Para registrar a nuevos usuarios
    Implementamos una bandeja de entrada
    Que determina qué tipo de usuarios registrar

Antecedentes:
	Dado Un usuario no registrado

Escenario: Contacto inicial de un donatario
	Cuando Envia mensaje
		"""
		Necesito ayuda
		"""
	Entonces Recibe mensaje
		"""
		Gracias por tu mensaje! Por favor envianos una 
		foto de tu DNI para registrarte para recibir ayuda.
		"""
