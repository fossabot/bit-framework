﻿using Bit.Owin.Implementations;

namespace Bit.Test.Core.Implementations
{
    public class TestDependencyManager
    {
        public static AutofacTestDependencyManager CurrentTestDependencyManager
        {
            get => (AutofacTestDependencyManager)DefaultDependencyManager.Current;
            set => DefaultDependencyManager.Current = value;
        }
    }
}
