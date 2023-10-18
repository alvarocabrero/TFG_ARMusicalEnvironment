using System.Collections;
using System.Collections.Generic;
using System;
using System.Dynamic;
namespace OrkestraLib
{
    public class InstrumentMap{
        public string val ="";
        public Func<object,string> init ;
        public Func<object,string> on ;  
        public Func<object,string> off ; 
        public InstrumentMap(string value){
            this.val = value;
        }
          public InstrumentMap(){
            
        }
    }
}