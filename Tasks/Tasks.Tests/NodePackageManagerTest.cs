using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Isogeo.Build.Tasks.Tests
{

    public class NodePackageManagerTest
    {

        [Fact]
        public void Action_ShouldBeInstallByDefault()
        {
            var npm=new NodePackageManager();

            Assert.Equal("install", npm.Action);
        }

        [Fact]
        public void Action_ShouldAssignCorrectValues()
        {
            var npm=new NodePackageManager();

            npm.Action=NpmAction.Update.ToString();
            Assert.Equal( "update", npm.Action );

            npm.Action=NpmAction.RunScript.ToString();
            Assert.Equal( "run-script", npm.Action );
        }

    }
}
