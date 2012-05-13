﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RobustHaven.Text.Npeg
{
    // Understand that node replacement should not have any logic this.Parent 
    // since full tree is not yet created.
    public abstract class IAstNodeReplacement : AstNode, IAstNodeVisitor
    {
        public abstract void VisitEnter(AstNode node);

        public abstract void VisitExecute(AstNode node);

        public abstract void VisitLeave(AstNode node);
    }
}
