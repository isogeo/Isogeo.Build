// $Id: SetEnvironmentVariable.cs 5162 2010-04-13 14:43:27Z mcartoixa $

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Isogeo.Build.Tasks
{
    public class SetEnvironmentVariable:
        Task
    {

        public override bool Execute()
        {
            Environment.SetEnvironmentVariable(_Variable, _Value, EnvironmentVariableTarget.Process);
            Log.LogMessage(MessageImportance.Low, "Environment variable %{0}% set to \"{1}\".", Variable, Value);
            return true;
        }

        [Required]
        public string Variable
        {
            get
            {
                return _Variable;
            }
            set
            {
                _Variable=value;
            }
        }

        [Required]
        public string Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value=value;
            }
        }

        private string _Variable;
        private string _Value;

    }
}
