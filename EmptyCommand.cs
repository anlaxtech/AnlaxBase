using AnlaxBase.Validate;
using AnlaxPackage;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AnlaxBase
{
    [Transaction(TransactionMode.Manual)]
    public class EmptyCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (StaticAuthorization.GetLiscence() == 0)
            {
                AuthSettingsBase auth = AuthSettingsBase.Initialize(true);
                NewValidate postgresSQLValidate = new NewValidate(auth.Login, auth.Password, commandData.Application.ActiveUIDocument.Document);
                postgresSQLValidate.CheckLicenseSilence();
            }
            if (App.LastAssembly!=null && !string.IsNullOrEmpty(App.LastNameClass))
            {
                InvokeRevitCommand(App.LastNameClass, commandData, ref message, elements, App.LastAssembly);
            }
            return Result.Succeeded;
        }

        public static void InvokeRevitCommand(string strCommandName, ExternalCommandData commandData, ref string message, object elements, Assembly fullPathDllName)
        {
            //Грузим нашу библиотеку в массив байтов.
            //Таким образом ревит ее не заблокирует на диске.
            try
            {
                Assembly objAssembly = fullPathDllName;

                //Проходимся по сборке.
                foreach (Type objType in objAssembly.GetTypes())
                {
                    //Выбираем класс.
                    if (objType.IsClass)
                    {
                        if (objType.IsSubclassOf(typeof(IApplicationStartAnlax)))
                        {
                            // Проверка на наличие статического поля UIControlledApplicationBase.
                            FieldInfo fieldInfo = objType.GetField("UIControlledApplicationBase",
                                                                  BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                            if (fieldInfo != null)
                            {
                                // Присвоение значения полю.
                                fieldInfo.SetValue(null, App.uiappStart);  // Используем null, т.к. поле статическое.
                            }
                        }
                        if (objType.Name.ToLower() == strCommandName.ToLower())
                        {
                            object ibaseObject = Activator.CreateInstance(objType);

                            object[] arguments = new object[] { commandData, message, elements };

                            //MethodInfo? mbinfo = objType.GetMethod("Execute");
                            //mbinfo.Invoke(ibaseObject, arguments);

                            object result = objType.InvokeMember(
                                "Execute",
                                BindingFlags.Default | BindingFlags.InvokeMethod,
                                null,
                                ibaseObject,
                                arguments);

                            break;
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {

                StringBuilder sb = new StringBuilder();
                foreach (Exception loaderException in ex.LoaderExceptions)
                {
                    sb.AppendLine(loaderException.Message);
                }
            }
        }
    }
}
