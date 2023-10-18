using System.Collections;
using System.Collections.Generic;
using System;
namespace OrkestraLib
{
    public class ContextItem{
        public string currentValue = "";
        public List<Action<string>> callbacks = new List<Action<string>>();
        public Func<object,string> on;
		public  Func<object,string> init;
        public Func<object,string> off;
	    public ContextItem(string currentValue, List<Action<string>> callbacks,  Func<object,string> on,  Func<object,string> off, Func<object,string> init){
		this.currentValue=currentValue;
		this.callbacks=callbacks;
		this.on=on;
		this.init=init;
		this.off=off;

	}
}
}
