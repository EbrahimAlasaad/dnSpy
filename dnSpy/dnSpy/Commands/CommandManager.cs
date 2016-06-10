﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	[Export(typeof(ICommandManager))]
	sealed class CommandManager : ICommandManager {
		readonly Lazy<ICommandInfoCreator, ICommandInfoCreatorMetadata>[] commandInfoCreators;
		readonly Lazy<ICommandTargetFilterCreator, ICommandTargetFilterCreatorMetadata>[] commandTargetFilterCreator;

		[ImportingConstructor]
		CommandManager([ImportMany] IEnumerable<Lazy<ICommandInfoCreator, ICommandInfoCreatorMetadata>> commandInfoCreators, [ImportMany] IEnumerable<Lazy<ICommandTargetFilterCreator, ICommandTargetFilterCreatorMetadata>> commandTargetFilterCreator) {
			this.commandInfoCreators = commandInfoCreators.OrderBy(a => a.Metadata.Order).ToArray();
			this.commandTargetFilterCreator = commandTargetFilterCreator.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public IRegisteredCommandElement Register(UIElement sourceElement, object target) {
			if (sourceElement == null)
				throw new ArgumentNullException(nameof(sourceElement));
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			var coll = new KeyShortcutCollection();
			foreach (var creator in commandInfoCreators)
				coll.Add(creator.Value, target);

			var cmdElem = new RegisteredCommandElement(this, sourceElement, coll, target);
			foreach (var c in commandTargetFilterCreator) {
				var filter = c.Value.Create(target);
				if (filter == null)
					continue;
				cmdElem.AddFilter(filter, c.Metadata.Order);
			}
			return cmdElem;
		}

		public CommandInfo? CreateCommandInfo(object target, string text) {
			foreach (var c in commandInfoCreators) {
				var c2 = c.Value as ICommandInfoCreator2;
				if (c2 == null)
					continue;
				var cmd = c2.CreateFromTextInput(target, text);
				if (cmd != null)
					return cmd;
			}
			return null;
		}
	}
}
