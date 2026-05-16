// Code for hybrid TAC
private string GenerateIRFromAST(List<VectorDeclNode> astNodes)
{
    if (astNodes == null || astNodes.Count == 0)
        return "IR не сгенерирован: AST пуст.";

    StringBuilder sb = new StringBuilder();
    sb.AppendLine("Виртуальные инструкции ДО оптимизаций");
    sb.AppendLine();

    foreach (var node in astNodes)
    {
        if (node.Initializer is FuncCallNode funcCall && funcCall.FunctionName == "c")
        {
            int argCount = funcCall.Arguments.Count;
            sb.AppendLine("// Выделение временных переменных для каждого аргумента");

            for (int i = 0; i < argCount; i++)
            {
                string value = GetArgValueForIR(funcCall.Arguments[i]);
                sb.AppendLine($"t{i + 1} = {value}");
            }
            sb.AppendLine();

            sb.AppendLine("// Создание списка аргументов (массива указателей)");
            sb.AppendLine($"arg_list = ALLOC_LIST({argCount})");
            for (int i = 0; i < argCount; i++)
            {
                sb.AppendLine($"SET_LIST_ELT(arg_list, {i}, t{i + 1})");
            }
            sb.AppendLine();

            sb.AppendLine("// Вызов функции c() с аргументами");
            sb.AppendLine($"tmp_result = CALL_FUNCTION(c, arg_list, {argCount})");
            sb.AppendLine();

            sb.AppendLine("// Внутри c(): определение результирующего типа");
            sb.AppendLine("type_max = 0");
            var types = funcCall.Arguments.Select(a => GetArgType(a)).ToList();
            string maxType = GetMaxType(types);
            sb.AppendLine($"// Результат: type_max = {maxType}");
            sb.AppendLine();

            sb.AppendLine("// Подсчет элементов (исключая NULL)");
            int nonNullCount = funcCall.Arguments.Count(a => !(a is NullLiteralNode));
            sb.AppendLine($"count = {nonNullCount}  // NULL удалены из подсчета");
            sb.AppendLine();

            sb.AppendLine("// Выделение памяти для результата");
            sb.AppendLine($"result = ALLOC_VECTOR({maxType}, count)");
            sb.AppendLine();

            sb.AppendLine("// Копирование с приведением типов");
            sb.AppendLine("pos = 0");
            int pos = 1;
            for (int i = 0; i < funcCall.Arguments.Count; i++)
            {
                var arg = funcCall.Arguments[i];
                if (arg is NullLiteralNode)
                {
                    sb.AppendLine($"// t{i + 1}: NULL - пропускаем");
                }
                else
                {
                    string argValue = GetArgValueForIR(arg);
                    string argType = GetArgType(arg);
                    if (argType != "STRSXP")
                    {
                        sb.AppendLine($"// t{i + 1}: {argValue} ({argType}) → \"{GetCoercedValue(arg)}\" (STRSXP)");
                    }
                    else
                    {
                        sb.AppendLine($"// t{i + 1}: {argValue} (STRSXP) → остается {argValue}");
                    }
                    sb.AppendLine($"temp_char{pos} = COERCE_TO_CHAR(t{i + 1})");
                    sb.AppendLine($"SET_VECTOR_ELT(result, {pos - 1}, temp_char{pos})");
                    pos++;
                }
            }
            sb.AppendLine();

            sb.AppendLine("// Присваивание переменной");
            sb.AppendLine($"{node.Name} = result");
            sb.AppendLine();

            sb.AppendLine("// Освобождение временных переменных");
            sb.AppendLine("FREE(arg_list)");
            sb.AppendLine($"FREE({string.Join(", ", Enumerable.Range(1, argCount).Select(i => $"t{i}"))})");
            if (nonNullCount > 0)
                sb.AppendLine($"FREE({string.Join(", ", Enumerable.Range(1, nonNullCount).Select(i => $"temp_char{i}"))})");
        }
        else if (node.Initializer == null && node.IsNull)
        {
            sb.AppendLine($"// NULL-присваивание");
            sb.AppendLine($"{node.Name} = NULL");
        }
    }

    return sb.ToString();
}

private string GetArgValueForIR(AstNode arg)
{
    switch (arg)
    {
        case NumberLiteralNode num:
            return $"{num.Value}  // {num.Type}";
        case LogicalLiteralNode logical:
            return $"{logical.Value.ToString().ToUpper()}  // logical";
        case CharacterLiteralNode character:
            return $"\"{character.Value}\"  // character";
        case NullLiteralNode:
            return "NULL  // NULL";
        default:
            return "UNKNOWN";
    }
}

private string GetArgType(AstNode arg)
{
    switch (arg)
    {
        case NumberLiteralNode num:
            return num.Type == "integer" ? "INTSXP" : "REALSXP";
        case LogicalLiteralNode:
            return "LGLSXP";
        case CharacterLiteralNode:
            return "STRSXP";
        case NullLiteralNode:
            return "NILSXP";
        default:
            return "UNKNOWN";
    }
}

private string GetMaxType(List<string> types)
{
    var priority = new Dictionary<string, int>
    {
        {"NILSXP", 0},
        {"LGLSXP", 1},
        {"INTSXP", 2},
        {"REALSXP", 3},
        {"STRSXP", 4}
    };

    string maxType = "NILSXP";
    int maxPriority = 0;

    foreach (var t in types)
    {
        if (priority.ContainsKey(t) && priority[t] > maxPriority)
        {
            maxPriority = priority[t];
            maxType = t;
        }
    }

    return maxType;
}

private string GetCoercedValue(AstNode arg)
{
    switch (arg)
    {
        case NumberLiteralNode num:
            return num.Value;
        case LogicalLiteralNode logical:
            return logical.Value ? "TRUE" : "FALSE";
        default:
            return "";
    }
}


private string GenerateOptimizedIR_RemoveNull(List<VectorDeclNode> astNodes)
{
    if (astNodes == null || astNodes.Count == 0)
        return "IR не сгенерирован: AST пуст.";

    StringBuilder sb = new StringBuilder();
    sb.AppendLine("Виртуальные инструкции ПОСЛЕ ОПТИМИЗАЦИИ 1 (удаление NULL)");
    sb.AppendLine();

    foreach (var node in astNodes)
    {
        if (node.Initializer is FuncCallNode funcCall && funcCall.FunctionName == "c")
        {
            var nonNullArgs = funcCall.Arguments.Where(a => !(a is NullLiteralNode)).ToList();
            int argCount = nonNullArgs.Count;

            sb.AppendLine("// NULL удалены");
            sb.AppendLine();

            sb.AppendLine("// Выделение временных переменных для каждого аргумента");
            for (int i = 0; i < argCount; i++)
            {
                string value = GetArgValueForIR(nonNullArgs[i]);
                sb.AppendLine($"t{i + 1} = {value}");
            }
            sb.AppendLine();

            sb.AppendLine("// Создание списка аргументов");
            sb.AppendLine($"arg_list = ALLOC_LIST({argCount})");
            for (int i = 0; i < argCount; i++)
            {
                sb.AppendLine($"SET_LIST_ELT(arg_list, {i}, t{i + 1})");
            }
            sb.AppendLine();

            sb.AppendLine("// Вызов c() - все еще нужен из-за смешанных типов");
            sb.AppendLine($"tmp_result = CALL_FUNCTION(c, arg_list, {argCount})");
            sb.AppendLine();

            sb.AppendLine("// Внутри c(): определение типа");
            var types = nonNullArgs.Select(a => GetArgType(a)).ToList();
            string maxType = GetMaxType(types);
            sb.AppendLine($"// Результат: {maxType}");
            sb.AppendLine();

            sb.AppendLine($"result = ALLOC_VECTOR({maxType}, {argCount})");
            sb.AppendLine();

            sb.AppendLine("// Копирование с приведением");
            for (int i = 0; i < argCount; i++)
            {
                var arg = nonNullArgs[i];
                string argType = GetArgType(arg);
                if (argType != maxType)
                {
                    sb.AppendLine($"temp_char{i + 1} = COERCE_TO_CHAR(t{i + 1})  // приведение к {maxType}");
                    sb.AppendLine($"SET_VECTOR_ELT(result, {i}, temp_char{i + 1})");
                }
                else
                {
                    sb.AppendLine($"SET_VECTOR_ELT(result, {i}, t{i + 1})");
                }
            }
            sb.AppendLine();

            sb.AppendLine($"{node.Name} = result");
        }
        else if (node.Initializer == null && node.IsNull)
        {
            sb.AppendLine($"{node.Name} = NULL");
        }
    }

    return sb.ToString();
}

private string GenerateOptimizedIR_FullCoercion(List<VectorDeclNode> astNodes)
{
    if (astNodes == null || astNodes.Count == 0)
        return "IR не сгенерирован: AST пуст.";

    StringBuilder sb = new StringBuilder();
    sb.AppendLine("Виртуальные инструкции ПОСЛЕ ОПТИМИЗАЦИИ 2 (полное приведение типов)");
    sb.AppendLine();

    foreach (var node in astNodes)
    {
        if (node.Initializer is FuncCallNode funcCall && funcCall.FunctionName == "c")
        {
            var nonNullArgs = funcCall.Arguments.Where(a => !(a is NullLiteralNode)).ToList();
            int argCount = nonNullArgs.Count;

            var types = nonNullArgs.Select(a => GetArgType(a)).ToList();
            string targetType = GetMaxType(types);

            sb.AppendLine("// ВСЕ АРГУМЕНТЫ УЖЕ ОДНОГО ТИПА");
            sb.AppendLine($"// Целевой тип: {targetType}");
            sb.AppendLine();
            sb.AppendLine("// Оптимизация: вызов c() не нужен, создаем вектор напрямую");
            sb.AppendLine();

            sb.AppendLine("// Приведение значений к целевому типу");
            for (int i = 0; i < argCount; i++)
            {
                var arg = nonNullArgs[i];
                string value = GetCoercedToType(arg, targetType);
                sb.AppendLine($"c{i + 1} = {value}      // приведено к {targetType}");
            }
            sb.AppendLine();

            sb.AppendLine("// Прямое создание вектора без вызова c()");
            sb.AppendLine($"result = ALLOC_VECTOR({targetType}, {argCount})");
            for (int i = 0; i < argCount; i++)
            {
                sb.AppendLine($"SET_VECTOR_ELT(result, {i}, c{i + 1})");
            }
            sb.AppendLine();

            sb.AppendLine($"{node.Name} = result");
            sb.AppendLine();
            sb.AppendLine("// Преимущества:");
            sb.AppendLine("// - Нет вызова функции c()");
            sb.AppendLine("// - Нет временного списка arg_list");
            sb.AppendLine("// - Нет проверок типов во время выполнения");
            sb.AppendLine("// - Нет операций приведения типов (всё сделано на этапе анализа)");
        }
    }

    return sb.ToString();
}

private string GetCoercedToType(AstNode arg, string targetType)
{
    switch (targetType)
    {
        case "STRSXP":
            switch (arg)
            {
                case NumberLiteralNode num:
                    return $"\"{num.Value}\"";
                case LogicalLiteralNode logical:
                    return $"\"{logical.Value.ToString().ToUpper()}\"";
                case CharacterLiteralNode character:
                    return $"\"{character.Value}\"";
                default:
                    return "UNKNOWN";
            }
        case "REALSXP":
            switch (arg)
            {
                case NumberLiteralNode num:
                    return num.Value;
                case LogicalLiteralNode logical:
                    return logical.Value ? "1.0" : "0.0";
                default:
                    return "UNKNOWN";
            }
        default:
            return GetArgValueForIR(arg);
    }
}

private string CountOperations(string tacText)
{
    if (string.IsNullOrEmpty(tacText))
        return "Нет данных";

    int allocCount = 0;
    int setCount = 0;
    int callCount = 0;
    int coercionCount = 0;
    int freeCount = 0;

    var lines = tacText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

    foreach (var line in lines)
    {
        if (line.Contains("ALLOC_LIST") || line.Contains("ALLOC_VECTOR"))
            allocCount++;
        else if (line.Contains("SET_LIST_ELT") || line.Contains("SET_VECTOR_ELT"))
            setCount++;
        else if (line.Contains("CALL_FUNCTION"))
            callCount++;
        else if (line.Contains("COERCE_TO_CHAR"))
            coercionCount++;
        else if (line.Contains("FREE"))
            freeCount++;
    }

    return $"ALLOC: {allocCount}, SET: {setCount}, CALL: {callCount}, COERCE: {coercionCount}, FREE: {freeCount}";
}

private void RunFullOptimization(TextBox outputBox)
{
    if (lastAstNodes == null || lastAstNodes.Count == 0)
    {
        outputBox.Text = "Нет AST. Сначала выполните анализ (Пуск).";
        return;
    }

    StringBuilder result = new StringBuilder();

    string originalTac = GenerateIRFromAST(lastAstNodes);
    result.AppendLine(originalTac);
    result.AppendLine($"СТАТИСТИКА ДО ОПТИМИЗАЦИЙ");
    result.AppendLine(CountOperations(originalTac));
    result.AppendLine();
    result.AppendLine(new string('=', 60));
    result.AppendLine();

    string opt1Tac = GenerateOptimizedIR_RemoveNull(lastAstNodes);
    result.AppendLine(opt1Tac);
    result.AppendLine($"СТАТИСТИКА ПОСЛЕ ОПТИМИЗАЦИИ 1");
    result.AppendLine(CountOperations(opt1Tac));
    result.AppendLine();
    result.AppendLine(new string('=', 60));
    result.AppendLine();

    string opt2Tac = GenerateOptimizedIR_FullCoercion(lastAstNodes);
    result.AppendLine(opt2Tac);
    result.AppendLine($"СТАТИСТИКА ПОСЛЕ ОПТИМИЗАЦИИ 2");
    result.AppendLine(CountOperations(opt2Tac));

    outputBox.Text = result.ToString();
}
