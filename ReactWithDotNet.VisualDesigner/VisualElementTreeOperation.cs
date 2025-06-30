namespace ReactWithDotNet.VisualDesigner;

record VisualElementTreeOperationMoveResponse
{
    public VisualElementModel NewRoot { get; init; }

    public ApplicationSelectionState Selection { get; init; }
}

static class VisualElementTreeOperation
{
    public static Result<VisualElementTreeOperationMoveResponse> Move(VisualElementModel root, string source, string target, DragPosition position)
    {
        // root check
        {
            if (source == "0")
            {
                return new Exception("Root node cannot move.");
            }
        }

        // parent - child control
        {
            if (target.StartsWith(source, StringComparison.OrdinalIgnoreCase))
            {
                return new Exception("Parent node cannot add to child.");
            }
        }

        // same target control
        {
            if (source == target)
            {
                return new VisualElementTreeOperationMoveResponse();
            }
        }

        var isTryingToMakeRoot = target == "0" && position == DragPosition.Before;

        VisualElementModel sourceNodeParent;
        int sourceNodeIndex;
        {
            var temp = root;

            var indexArray = source.Split(',');

            var length = indexArray.Length - 1;
            for (var i = 1; i < length; i++)
            {
                temp = temp.Children[int.Parse(indexArray[i])];
            }

            sourceNodeIndex = int.Parse(indexArray[length]);

            sourceNodeParent = temp;
        }

        if (isTryingToMakeRoot)
        {
            var rootNode = sourceNodeParent.Children[sourceNodeIndex];

            sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

            rootNode.Children.Add(root);

            return new VisualElementTreeOperationMoveResponse
            {
                NewRoot = rootNode,
                Selection = new()
                {
                    VisualElementTreeItemPath = "0"
                }
            };
        }

        VisualElementModel targetNodeParent;
        int targetNodeIndex;
        {
            var temp = root;

            var indexArray = target.Split(',');

            var length = indexArray.Length - 1;
            for (var i = 1; i < length; i++)
            {
                temp = temp.Children[int.Parse(indexArray[i])];
            }

            targetNodeIndex = int.Parse(indexArray[length]);

            targetNodeParent = temp;
        }

        if (position == DragPosition.Inside)
        {
            var sourceNode = sourceNodeParent.Children[sourceNodeIndex];

            var targetNode = targetNodeParent.Children[targetNodeIndex];

            if (targetNode.Children.Count > 0)
            {
                return new Exception("Select valid location");
            }

            // remove from source
            sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

            if (targetNode.HasNoChild())
            {
                targetNode.Children.Add(sourceNode);

                return new VisualElementTreeOperationMoveResponse
                {
                    NewRoot   = root,
                    Selection = new()
                };
            }
        }

        // is same parent
        if (sourceNodeParent == targetNodeParent)
        {
            if (position == DragPosition.After && sourceNodeIndex - targetNodeIndex == 1)
            {
                return new VisualElementTreeOperationMoveResponse
                {
                    NewRoot = root
                };
            }

            if (position == DragPosition.Before && targetNodeIndex - sourceNodeIndex == 1)
            {
                return new VisualElementTreeOperationMoveResponse
                {
                    NewRoot = root
                };
            }
        }

        {
            var sourceNode = sourceNodeParent.Children[sourceNodeIndex];

            // remove from source
            sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

            if (sourceNodeParent == targetNodeParent)
            {
                // is adding end
                if (position == DragPosition.After && targetNodeIndex == targetNodeParent.Children.Count)
                {
                    targetNodeParent.Children.Insert(targetNodeIndex, sourceNode);

                    return new VisualElementTreeOperationMoveResponse
                    {
                        NewRoot   = root,
                        Selection = new()
                    };
                }

                if (position == DragPosition.After && targetNodeIndex == 0)
                {
                    targetNodeIndex++;
                }

                if (position == DragPosition.Before && targetNodeIndex == targetNodeParent.Children.Count)
                {
                    targetNodeIndex--;
                }
            }

            // insert into target
            targetNodeParent.Children.Insert(targetNodeIndex, sourceNode);

            return new VisualElementTreeOperationMoveResponse
            {
                NewRoot   = root,
                Selection = new()
            };
        }
    }
}