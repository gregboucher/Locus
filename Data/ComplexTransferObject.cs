﻿namespace Locus.Data
{
    public class ComplexTransferObject<T> : IComplexTransferObject<T> where T : class
    {
        public T Model { get; set; }
    }
}