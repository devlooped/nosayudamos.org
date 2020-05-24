#language: es-ES
Característica: Registracion de donatarios

Antecedentes:
    Dado Un storage limpio
    Y SendToSlackInDevelopment=true

Escenario: DNI con CUIL es aprobado
    Cuando Envia ID Content\CUIL2.jpg
    Entonces Recibe 'UI_Donee_Welcome'
    """
    Hola {0}, bienvenid{1} a la comunidad de Nos Ayudamos! INSTRUCCIONES
    """

Escenario: DNI con Monotributo Categoría A es aprobado
    Cuando Envia ID Content\CUIT+MonotributoA.jpg
    Entonces Recibe 'UI_Donee_Welcome'
    """
    Hola {0}, bienvenid{1} a la comunidad de Nos Ayudamos! INSTRUCCIONES
    """

Escenario: DNI con CUIT sin Monotributo es rechazado
    Cuando Envia ID Content\CUIT-Monotributo.jpg
    Entonces Recibe 'UI_Donee_NotApplicable'
    """
    Gracias {0} por tu mensaje. Actualmente estamos limitando la ayuda a personas 
    registradas en el régimen de Monotributo en la categoría A exclusivamente.
    """

Escenario: DNI con Monotributo Categoria Alta es rechazado
    Cuando Envia ID Content\CUIT+Monotributo.jpg
    Entonces Recibe 'UI_Donee_HighCategory'
    """
    Gracias {0} por tu mensaje. Actualmente estamos limitando la ayuda a personas 
    registradas en el régimen de Monotributo en la categoría A exclusivamente.
    """

Escenario: DNI con CUIT que paga Ganancias es rechazado
    Cuando Envia ID Content\CUIT+Ganancias.jpg
    Entonces Recibe 'UI_Donee_HasIncomeTax'
    """
    Gracias {0} por tu mensaje. Actualmente estamos limitando la ayuda a personas 
    que no tributan impuesto a las ganancias.
    """

Escenario: DNI sin código de barras solicita reintentar
    Cuando Envia ID Content\DNI-SinCodigo.jpg
    Entonces Recibe 'UI_Donee_ResendIdentifier1'
    """
    Recibimos tu mensaje, pero no pudimos reconocer tus datos en la imagen. 
    Podrías enviarnos una nueva foto del DNI, quizás con mejor iluminación o más 
    de cerca? Es importante que se vea con claridad el código de barras de la 
    esquina inferior derecha. Gracias por tu paciencia!
    """

Escenario: DNI sin código de barras por segunda vez solicita reintentar
    Cuando Envia ID Content\DNI-SinCodigo.jpg
    Y Envia ID Content\DNI-SinCodigo.jpg
    Entonces Recibe 'UI_Donee_ResendIdentifier2'
    """
    Eso tampoco funcionó :(. Hay mejores chances si la foto se saca con luz 
    natural, de día. Es importante también que no esté fuera de foco. 
    Gracias, y esperamos tu siguiente foto del DNI!
    """
