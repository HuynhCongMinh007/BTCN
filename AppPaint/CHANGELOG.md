## 🔲 TỐI ƯU: Rectangle với Snap-to-Square & Validation
- **Snap to Square**: Hold Shift → perfect square (width = height)
- **Corner Radius**: Support rounded corners với RadiusX/RadiusY
- **Validation**: Check minimum size, skip too small rectangles
- **Brush Caching**: Use GetBrush() cho performance
- **Smooth Corners**: StrokeLineJoin = Round
- **Anti-aliasing**: UseLayoutRounding = false

## 🎯 THÊM: Rectangle Helper Methods
- `SnapToSquare(start, end)` - snap to perfect square
- `GetRectangleDimensions(start, end)` - get (width, height)
- `GetRectangleArea(start, end)` - calculate area
- `GetRectanglePerimeter(start, end)` - calculate perimeter
- `GetRectangleInfo(start, end)` - format "W: XXpx H: YYpx A: ZZpx²"

## ⭕ TỐI ƯU: Ellipse/Circle với Validation & Performance
- **Brush Caching**: Use GetBrush() for stroke and fill
- **Validation**: Check minimum size, skip too small ellipses
- **Anti-aliasing**: UseLayoutRounding = false
- **Circle Mode**: Auto-snap to perfect circle (width = height)

## 🎯 THÊM: Ellipse/Circle Helper Methods
- `GetEllipseDimensions(start, end, isCircle)` - get dimensions
- `GetEllipseArea(start, end, isCircle)` - calculate area (π × r1 × r2)
- `GetEllipseCircumference(start, end, isCircle)` - Ramanujan approximation
- `GetEllipseInfo(start, end, isCircle)` - format "⌀: XX R: YY A: ZZ" or "W: XX H: YY A: ZZ"

## △ TỐI ƯU: Triangle với Equilateral option & Quality
- **Brush Caching**: Use GetBrush() for performance
- **Smooth Corners**: StrokeLineJoin = Round
- **Anti-aliasing**: UseLayoutRounding = false
- **Equilateral Mode**: Optional perfect equilateral triangle

## 🎯 THÊM: Triangle Helper Methods
- `CalculateTrianglePoints(start, end, isEquilateral)` - compute points
- `GetTriangleArea(start, end)` - calculate area (base × height / 2)
- `GetTrianglePerimeter(start, end)` - calculate perimeter
- `GetTriangleInfo(start, end)` - format "Base: XX H: YY A: ZZ"

## ⬟ TỐI ƯU: Polygon với Performance & Measurements
- **Brush Caching**: Use GetBrush() for stroke and fill
- **Smooth Corners**: StrokeLineJoin = Round
- **Anti-aliasing**: UseLayoutRounding = false

## 🎯 THÊM: Polygon Helper Methods
- `GetPolygonArea(points)` - shoelace formula for area
- `GetPolygonPerimeter(points)` - sum of all sides
