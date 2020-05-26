﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:3.1.0.0
//      SpecFlow Generator Version:3.1.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace NosAyudamos
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.1.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class RegistracionDeDonatariosFeature : object, Xunit.IClassFixture<RegistracionDeDonatariosFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = ((string[])(null));
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "DoneeRegistration.feature"
#line hidden
        
        public RegistracionDeDonatariosFeature(RegistracionDeDonatariosFeature.FixtureData fixtureData, NosAyudamos_XUnitAssemblyFixture assemblyFixture, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("es-ES"), "Registracion de donatarios", null, ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public virtual void TestInitialize()
        {
        }
        
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        public virtual void FeatureBackground()
        {
#line 4
#line hidden
#line 5
    testRunner.Given("Un storage limpio", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Dado ");
#line hidden
#line 7
    testRunner.And("SendToSlackInDevelopment=false", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Y ");
#line hidden
#line 9
    testRunner.And("SendToMessagingInDevelopment=false", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Y ");
#line hidden
        }
        
        void System.IDisposable.Dispose()
        {
            this.TestTearDown();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="DNI legible")]
        [Xunit.TraitAttribute("FeatureTitle", "Registracion de donatarios")]
        [Xunit.TraitAttribute("Description", "DNI legible")]
        public virtual void DNILegible()
        {
            string[] tagsOfScenario = ((string[])(null));
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("DNI legible", null, ((string[])(null)));
#line 11
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 4
this.FeatureBackground();
#line hidden
#line 12
    testRunner.When("Envia ID Content\\CUIL.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Cuando ");
#line hidden
#line 13
    testRunner.Then("Recibe \'UI_Donee_Welcome\'", "Hola {0}, bienvenid{1} a la comunidad de Nos Ayudamos! INSTRUCCIONES", ((TechTalk.SpecFlow.Table)(null)), "Entonces ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="DNI sin código de barras solicita reintentar")]
        [Xunit.TraitAttribute("FeatureTitle", "Registracion de donatarios")]
        [Xunit.TraitAttribute("Description", "DNI sin código de barras solicita reintentar")]
        public virtual void DNISinCodigoDeBarrasSolicitaReintentar()
        {
            string[] tagsOfScenario = ((string[])(null));
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("DNI sin código de barras solicita reintentar", null, ((string[])(null)));
#line 18
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 4
this.FeatureBackground();
#line hidden
#line 19
    testRunner.When("Envia ID Content\\DNI-SinCodigo.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Cuando ");
#line hidden
#line 20
    testRunner.Then("Recibe \'UI_Donee_ResendIdentifier1\'", @"Recibimos tu mensaje, pero no pudimos reconocer tus datos en la imagen. 
Podrías enviarnos una nueva foto del DNI, quizás con mejor iluminación o más 
de cerca? Es importante que cuando tomes la foto, el DNI ocupe la totalidad 
de la pantalla de tu celular. Gracias por tu paciencia!", ((TechTalk.SpecFlow.Table)(null)), "Entonces ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="DNI sin código de barras por segunda vez solicita reintentar")]
        [Xunit.TraitAttribute("FeatureTitle", "Registracion de donatarios")]
        [Xunit.TraitAttribute("Description", "DNI sin código de barras por segunda vez solicita reintentar")]
        public virtual void DNISinCodigoDeBarrasPorSegundaVezSolicitaReintentar()
        {
            string[] tagsOfScenario = ((string[])(null));
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("DNI sin código de barras por segunda vez solicita reintentar", null, ((string[])(null)));
#line 28
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 4
this.FeatureBackground();
#line hidden
#line 29
    testRunner.When("Envia ID Content\\DNI-SinCodigo.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Cuando ");
#line hidden
#line 30
    testRunner.And("Envia ID Content\\DNI-SinCodigo.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Y ");
#line hidden
#line 31
    testRunner.Then("Recibe \'UI_Donee_ResendIdentifier2\'", @"Sigo sin poder reconocer tus datos :(. Intentemos una última vez antes 
de involucrar a las personas de soporte técnico, que siempre aprecian 
que agotemos nuestras posibilidades primero. Buscá si es posible un lugar 
sin sombra y con luz natural (cuanta más, mejor!), y recordá que el DNI 
debería ocupar toda la pantalla de tu aplicación de cámara de fotos. 
Vamos que la tercera es la vencida!", ((TechTalk.SpecFlow.Table)(null)), "Entonces ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="DNI sin código de barras por tercera vez avisa a humanos")]
        [Xunit.TraitAttribute("FeatureTitle", "Registracion de donatarios")]
        [Xunit.TraitAttribute("Description", "DNI sin código de barras por tercera vez avisa a humanos")]
        public virtual void DNISinCodigoDeBarrasPorTerceraVezAvisaAHumanos()
        {
            string[] tagsOfScenario = ((string[])(null));
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("DNI sin código de barras por tercera vez avisa a humanos", null, ((string[])(null)));
#line 41
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 4
this.FeatureBackground();
#line hidden
#line 42
    testRunner.When("Envia ID Content\\DNI-SinCodigo.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Cuando ");
#line hidden
#line 43
    testRunner.And("Envia ID Content\\DNI-SinCodigo.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Y ");
#line hidden
#line 44
    testRunner.And("Envia ID Content\\DNI-SinCodigo.jpg", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Y ");
#line hidden
#line 45
    testRunner.Then("Recibe \'UI_Donee_RegistrationFailed\'", "Lamento tener problemas para reconocer tu DNI. Ya avisé a los seres \r\nhumanos que" +
                        " programaron esto para que me ayuden. Se van a contactar \r\ncon vos a la brevedad" +
                        ". Perdón y gracias de nuevo por tu paciencia!", ((TechTalk.SpecFlow.Table)(null)), "Entonces ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.1.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                RegistracionDeDonatariosFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                RegistracionDeDonatariosFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
