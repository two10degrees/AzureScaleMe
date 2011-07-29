using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spring.Context;
using Spring.Context.Support;
using Spring;

namespace Two10.AzureScaleMe
{
    class Spring
    {

        static Spring()
        {
        
        }

        public static T Create<T>(string name) where T : class
        {
            IApplicationContext context = ContextRegistry.GetContext();
            T obj = context.GetObject(name, typeof(T), null) as T;
            
            return obj;
        }

    }
}
