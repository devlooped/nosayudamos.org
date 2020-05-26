#language: es-ES
Característica: Registracion de donatarios

Antecedentes:
    Dado Un storage limpio
    # Por las fotos en storage local
    Y SendToSlackInDevelopment=false
    # Porque utilizamos test #s
    Y SendToMessagingInDevelopment=false

Escenario: DNI legible
    Cuando Envia ID Content\CUIL.jpg
    Entonces Recibe 'UI_Donee_Welcome'
    """
    Hola {0}, bienvenid{1} a la comunidad de Nos Ayudamos! INSTRUCCIONES
    """

Escenario: DNI sin código de barras solicita reintentar
    Cuando Envia ID Content\DNI-SinCodigo.jpg
    Entonces Recibe 'UI_Donee_ResendIdentifier1'
    """
    Recibimos tu mensaje, pero no pudimos reconocer tus datos en la imagen. 
    Podrías enviarnos una nueva foto del DNI, quizás con mejor iluminación o más 
    de cerca? Es importante que cuando tomes la foto, el DNI ocupe la totalidad 
    de la pantalla de tu celular. Gracias por tu paciencia!
    """

Escenario: DNI sin código de barras por segunda vez solicita reintentar
    Cuando Envia ID Content\DNI-SinCodigo.jpg
    Y Envia ID Content\DNI-SinCodigo.jpg
    Entonces Recibe 'UI_Donee_ResendIdentifier2'
    """
    Sigo sin poder reconocer tus datos :(. Intentemos una última vez antes 
    de involucrar a las personas de soporte técnico, que siempre aprecian 
    que agotemos nuestras posibilidades primero. Buscá si es posible un lugar 
    sin sombra y con luz natural (cuanta más, mejor!), y recordá que el DNI 
    debería ocupar toda la pantalla de tu aplicación de cámara de fotos. 
    Vamos que la tercera es la vencida!
    """

Escenario: DNI sin código de barras por tercera vez avisa a humanos
    Cuando Envia ID Content\DNI-SinCodigo.jpg
    Y Envia ID Content\DNI-SinCodigo.jpg
    Y Envia ID Content\DNI-SinCodigo.jpg
    Entonces Recibe 'UI_Donee_RegistrationFailed'
    """
    Lamento tener problemas para reconocer tu DNI. Ya avisé a los seres 
    humanos que programaron esto para que me ayuden. Se van a contactar 
    con vos a la brevedad. Perdón y gracias de nuevo por tu paciencia!
    """
