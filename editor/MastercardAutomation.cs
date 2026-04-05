using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace editor
{
    public class MasterCardAutomaton
    {
        private enum State
        {
            START,
            PREFIX_5,
            PREFIX_2,
            PREFIX_22, 
            PREFIX_27, 
            PREFIX_DD,
            PREFIX_272,
            PREFIX_222,
            PREFIX_2DD,
            PREFIX_DDD,
            PREFIX_4D,
            DIGITS_1_12,
            ACCEPT
        }

        private State currentState;
        private int digitCount;
        private int startPosition;
        private string currentMatch;

        public MasterCardAutomaton()
        {
            Reset();
        }

        public void Reset()
        {
            currentState = State.START;
            digitCount = 0;
            startPosition = -1;
            currentMatch = "";
        }

        public bool ProcessChar(char c, int globalPos, bool isDigit, out string match, out int matchStart, out int matchLength)
        {
            match = null;
            matchStart = -1;
            matchLength = 0;

            if (!isDigit)
            {
                if (currentState == State.ACCEPT || digitCount == 16)
                {
                    match = currentMatch;
                    matchStart = startPosition;
                    matchLength = currentMatch.Length;
                    Reset();
                    return true;
                }
                Reset();
                return false;
            }

            switch (currentState)
            {
                case State.START:
                    if (c == '5')
                    {
                        currentState = State.PREFIX_5;
                        startPosition = globalPos;
                        currentMatch = c.ToString();
                        digitCount = 1;
                    }
                    else if (c == '2')
                    {
                        currentState = State.PREFIX_2;
                        startPosition = globalPos;
                        currentMatch = c.ToString();
                        digitCount = 1;
                    }
                    break;

                case State.PREFIX_5:
                    if (c >= '1' && c <= '5')
                    {
                        currentState = State.PREFIX_DD;
                        currentMatch += c;
                        digitCount++;
                    }
                    else
                    {
                        Reset();
                        if (c == '5') ProcessChar(c, globalPos, isDigit, out match, out matchStart, out matchLength);
                    }
                    break;

                case State.PREFIX_DD:
                    currentMatch += c;
                    digitCount++;
                    break;

                case State.PREFIX_2:
                    if (c == '2')
                    {
                        currentState = State.PREFIX_22;
                        currentMatch += c;
                        digitCount++;
                    }
                    else if (c == '7')
                    {
                        currentState = State.PREFIX_27;
                        currentMatch += c;
                        digitCount++;
                    }
                    else if (c >= '3' && c <= '6')
                    {
                        currentState = State.PREFIX_DD;
                        currentMatch += c;
                        digitCount++;
                    }
                    else
                    {
                        Reset();
                        if (c == '2' || c == '5') ProcessChar(c, globalPos, isDigit, out match, out matchStart, out matchLength);
                    }
                    break;

                case State.PREFIX_22:
                    if (c == '2')
                    {
                        currentState = State.PREFIX_222;
                        currentMatch += c;
                        digitCount++;
                    }
                    else if (c >= '3' && c <= '9')
                    {
                        currentState = State.PREFIX_2DD;
                        currentMatch += c;
                        digitCount++;
                    }
                    else
                    {
                        Reset();
                    }
                    break;

                case State.PREFIX_222:
                    if (c >= '1' && c <= '9')
                    {
                        currentState = State.PREFIX_4D;
                        currentMatch += c;
                        digitCount++;
                    }
                    else
                    {
                        Reset();
                    }
                    break;

                case State.PREFIX_27:
                    if (c == '2')
                    {
                        currentState = State.PREFIX_272;
                        currentMatch += c;
                        digitCount++;
                    }
                    else if (c >= '0' && c <= '1')
                    {
                        currentState = State.PREFIX_2DD;
                        currentMatch += c;
                        digitCount++;
                    }
                    else
                    {
                        Reset();
                    }
                    break;

                case State.PREFIX_272:
                    if (c == '0')
                    {
                        currentState = State.PREFIX_4D;
                        currentMatch += c;
                        digitCount++;
                    }
                    else
                    {
                        Reset();
                    }
                    break;

                case State.PREFIX_2DD:
                    currentState = State.PREFIX_4D;
                    currentMatch += c;
                    digitCount++;
                    break;

                case State.PREFIX_DDD:
                    currentState = State.PREFIX_4D;
                    currentMatch += c;
                    digitCount++;
                    break;

                case State.PREFIX_4D:
                    currentState = State.DIGITS_1_12;
                    currentMatch += c;
                    digitCount++;
                    break;

                case State.DIGITS_1_12:
                    currentMatch += c;
                    digitCount++;
                    if (digitCount == 16)
                    {
                        currentState = State.ACCEPT;
                        match = currentMatch;
                        matchStart = startPosition;
                        matchLength = currentMatch.Length;
                        Reset();
                        return true;
                    }
                    break;

                default:
                    if (digitCount < 16 && isDigit)
                    {
                        currentMatch += c;
                        digitCount++;
                        if (digitCount == 16)
                        {
                            currentState = State.ACCEPT;
                            match = currentMatch;
                            matchStart = startPosition;
                            matchLength = currentMatch.Length;
                            Reset();
                            return true;
                        }
                    }
                    else
                    {
                        Reset();
                        if (c == '5' || c == '2') ProcessChar(c, globalPos, isDigit, out match, out matchStart, out matchLength);
                    }
                    break;
            }

            return false;
        }
    }
}
