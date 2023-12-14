using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace DotNetty.Common.Utilities
{
    internal struct CharSequenceEnumerator : IEnumerator<char>
    {
        private ICharSequence charSequence;
        private int index;
        private char currentElement;

        internal CharSequenceEnumerator(ICharSequence charSequence)
        {
            Contract.Requires(charSequence != null);

            this.charSequence = charSequence;
            this.index = -1;
            this.currentElement = (char)0;
        }

        public bool MoveNext()
        {
            if (this.index < this.charSequence.Count - 1)
            {
                this.index++;
                this.currentElement = this.charSequence[this.index];
                return true;
            }

            this.index = this.charSequence.Count;
            return false;
        }

        object IEnumerator.Current
        {
            get
            {
                if (this.index == -1)
                {
                    throw new InvalidOperationException("Enumerator not initialized.");
                }
                if (this.index >= this.charSequence.Count)
                {
                    throw new InvalidOperationException("Eumerator already completed.");
                }
                return this.currentElement;
            }
        }

        public char Current
        {
            get
            {
                if (this.index == -1)
                {
                    throw new InvalidOperationException("Enumerator not initialized.");
                }
                if (this.index >= this.charSequence.Count)
                {
                    throw new InvalidOperationException("Eumerator already completed.");
                }
                return this.currentElement;
            }
        }

        public void Reset()
        {
            this.index = -1;
            this.currentElement = (char)0;
        }

        public void Dispose()
        {
            if (this.charSequence != null)
            {
                this.index = this.charSequence.Count;
            }
            this.charSequence = null;
        }
    }
}
