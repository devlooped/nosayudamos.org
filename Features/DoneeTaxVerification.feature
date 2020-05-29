#language: es-ES
Característica: Verificacion de donatarios

Antecedentes:
    Dado Un storage limpio
    Y SendToSlackInDevelopment=true
    # Porque utilizamos test phone #s
    Y SendToMessagingInDevelopment=false

Escenario: DNI con CUIL es aprobado
    Cuando Envia ID Content\CUIL2.jpg
    Entonces Recibe 'UI_Donee_Welcome'
    """
    Hola {0}, bienvenid{1} a la comunidad de Nos Ayudamos!
    """

Escenario: DNI con Monotributo Categoría A es aprobado
    Cuando Envia ID Content\CUIT+MonotributoA.jpg
    Entonces Recibe 'UI_Donee_Welcome'
    """
    Hola {0}, bienvenid{1} a la comunidad de Nos Ayudamos!
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
