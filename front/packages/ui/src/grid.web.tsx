/*
 * Kyoo - A portable and vast media library solution.
 * Copyright (c) Kyoo.
 *
 * See AUTHORS.md and LICENSE file in the project root for full license information.
 *
 * Kyoo is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * any later version.
 *
 * Kyoo is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Kyoo. If not, see <https://www.gnu.org/licenses/>.
 */

import { ReactNode, useRef } from "react";
import { Layout } from "./fetch";
import { useVirtualizer } from "@tanstack/react-virtual";

export const Grid = <T extends { id: string }>({
	data,
	children,
	layout,
	getItemSize,
	...props
}: {
	data: T[];
	children: (item: T, index: number) => ReactNode;
	layout: Layout;
	getItemSize: (item: T, index: number) => number;
}) => {
	const ref = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		// horizontal: layout.layout === "horizontal",
		count: data.length,
		getScrollElement: () => ref.current,
		estimateSize: (i) => 350, //getItemSize(data[i], i),
		overscan: 5,
	});
	console.log(virtualizer);
	console.log(virtualizer.getTotalSize());

	return (
		<div ref={ref} style={{ width: 400, height: 600, overflow: "auto" }}>
			<div
				style={{
					height: virtualizer.getTotalSize(),
					width: "100%",
					position: "relative",
				}}
				// {...props}
			>
				{virtualizer.getVirtualItems().map((x) => (
					<div
						key={x.key}
						data-index={x.index}
						ref={virtualizer.measureElement}
						style={{
							position: "absolute",
							top: 0,
							left: 0,
							width: "100%",
							// height: `${x.size}px`,
							transform: `translateY(${x.start}px)`,
						}}
					>
						{children(data[x.index], x.index)}
					</div>
				))}
			</div>
		</div>
	);
};
