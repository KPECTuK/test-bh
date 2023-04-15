using System;
using System.Diagnostics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BH.Components
{
	[Serializable]
	public struct CxFrustum
	{
		public Vector3 Source;
		public Vector3 Forward;

		public Vector3 VUR;
		public Vector3 VDR;
		public Vector3 VDL;
		public Vector3 VUL;

		public Plane Screen;
		public Plane Left;
		public Plane Right;
		public Plane Up;
		public Plane Down;
	}

	[Serializable]
	public struct CxOrigin
	{
		public Vector3 Location;
		public Quaternion Orientation;

		public static CxOrigin Identity =>
			new()
			{
				Orientation = Quaternion.identity,
				Location = Vector3.zero,
			};

		public override string ToString()
		{
			return $"{Location}";
		}

		//TODO: implement convert to matrix
	}

	[Serializable]
	public struct CxJoint
	{
		public Vector3 Source;
		public Vector3 Target;

		public Vector3 ToTarget => (Target - Source).normalized;

		public override string ToString()
		{
			return $"source: {Source}; target: {Target}";
		}
	}

	[Serializable]
	public struct CxSector
	{
		public Vector3 Origin;
		public Vector3 StartAtZero;
		public Vector3 StopAtZero;
	}

	public struct Meta<T>
	{
		public enum Shape
		{
			Point,
			Cross,
			Arrow,
			Circle,
			CurveKnot,
		}

		public Shape ShapeType;
		public Vector3 UpVector;
		public Color Color;
		public float Size;
		public float Duration;
		public float Dimpher;
		public bool IsGradient;

		public T Source;
	}

	public static class ExtensionsConvert
	{
		public static CxOrigin ToOrigin(this Transform source)
		{
			return source == null
				? CxOrigin.Identity
				: new CxOrigin
				{
					Location = source.position,
					Orientation = source.rotation,
				};
		}
	}

	public static class ExtensionsGizmos
	{
		#if UNITY_EDITOR
		public static readonly Texture2D BackCard = AssetDatabase
			.LoadAssetAtPath<Texture2D>("Assets/Editor/CxRes/msg.back.png");
		public static readonly Texture2D BackCaption = AssetDatabase
			.LoadAssetAtPath<Texture2D>("Assets/Editor/CxRes/msg.caption.back.png");
		public static readonly Texture2D BackGeneric = AssetDatabase
			.LoadAssetAtPath<Texture2D>("Assets/Editor/CxRes/general.back.png");
		public static readonly GUISkin SkinGeneric = AssetDatabase
			.LoadAssetAtPath<GUISkin>("Assets/Editor/CxRes/skin.tests.guiskin");
		#endif

		private const float AXIS_GAP = 0.7f;

		public static Meta<T> ToMeta<T>(this T source)
		{
			return new()
			{
				Source = source,
				//
				UpVector = Vector3.up,
				Size = .1f,
				Duration = 0f,
				Color = Color.yellow,
				Dimpher = 1f,
				ShapeType = Meta<T>.Shape.Cross,
				IsGradient = false,
			};
		}

		public static Meta<CxOrigin> ToMeta(this Vector3 source)
		{
			var pivot = new CxOrigin
			{
				Location = source,
				Orientation = Quaternion.identity,
			};
			return pivot.ToMeta();
		}

		public static Meta<CxOrigin> ToMeta(this Transform source)
		{
			return source.ToOrigin().ToMeta();
		}

		public static Meta<T> SetSize<T>(this Meta<T> source, float value)
		{
			source.Size = value;
			return source;
		}

		public static Meta<T> SetColor<T>(this Meta<T> source, Color value)
		{
			source.Color = value;
			return source;
		}

		public static Meta<T> SetDuration<T>(this Meta<T> source, float value)
		{
			source.Duration = value;
			return source;
		}

		public static Meta<T> SetShape<T>(this Meta<T> source, Meta<T>.Shape value)
		{
			source.ShapeType = value;
			return source;
		}

		public static Meta<T> SetGradient<T>(this Meta<T> source, bool value)
		{
			source.IsGradient = value;
			return source;
		}

		public static Meta<T> SetUpVector<T>(this Meta<T> source, Vector3 value)
		{
			source.UpVector = value;
			return source;
		}

		// 

		public static void Draw(this Vector3[] source, Color color)
		{
			for(var index = 0; index < source.Length; index++)
			{
				var next = (index + 1) % source.Length;
				UnityEngine.Debug.DrawLine(source[index], source[next], color);
			}
		}

		public static void Draw(this Plane source, Color color)
		{
			var origin = source.normal * -source.distance;
			const float STEP = Mathf.PI / 12f;
			var unit = Quaternion.FromToRotation(Vector3.up, source.normal) * Vector3.left;
			for(var delta = 0f; delta < 2f * Mathf.PI; delta += Mathf.PI / 12f)
			{
				UnityEngine.Debug.DrawLine(
					origin + Quaternion.AngleAxis(delta * Mathf.Rad2Deg, source.normal) * unit,
					origin + Quaternion.AngleAxis((delta + STEP) * Mathf.Rad2Deg, source.normal) * unit,
					color);
			}

			UnityEngine.Debug.DrawLine(origin, origin + source.normal * 1.2f, Color.cyan);
			//origin.DrawCross(Quaternion.LookRotation(source.normal), color);
			//origin.DrawCross(Quaternion.identity, color);
		}

		public static void Draw(this Ray source, Color color)
		{
			UnityEngine.Debug.DrawLine(source.origin, source.origin + source.direction, color);
		}

		public static void DrawPoint(this Meta<CxOrigin> source)
		{
			var oneOpposite = new Vector3(-1f, 1f);
			UnityEngine.Debug.DrawLine(
				source.Source.Location + Vector3.one * source.Size,
				source.Source.Location - Vector3.one * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location + oneOpposite * source.Size,
				source.Source.Location - oneOpposite * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
		}

		public static void DrawCross(this Meta<CxOrigin> source)
		{
			UnityEngine.Debug.DrawLine(
				source.Source.Location + source.Source.Orientation * Vector3.up * source.Size * AXIS_GAP,
				source.Source.Location + source.Source.Orientation * Vector3.up * source.Size,
				Color.Lerp(Color.black, Color.green, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location + source.Source.Orientation * Vector3.up * source.Size * AXIS_GAP,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location - source.Source.Orientation * Vector3.up * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);

			UnityEngine.Debug.DrawLine(
				source.Source.Location + source.Source.Orientation * Vector3.right * source.Size * AXIS_GAP,
				source.Source.Location + source.Source.Orientation * Vector3.right * source.Size,
				Color.Lerp(Color.black, Color.red, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location + source.Source.Orientation * Vector3.right * source.Size * AXIS_GAP,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location - source.Source.Orientation * Vector3.right * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);

			UnityEngine.Debug.DrawLine(
				source.Source.Location + source.Source.Orientation * Vector3.forward * source.Size * AXIS_GAP,
				source.Source.Location + source.Source.Orientation * Vector3.forward * source.Size,
				Color.Lerp(Color.black, Color.blue, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location + source.Source.Orientation * Vector3.forward * source.Size * AXIS_GAP,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
			UnityEngine.Debug.DrawLine(
				source.Source.Location,
				source.Source.Location - source.Source.Orientation * Vector3.forward * source.Size,
				Color.Lerp(Color.black, source.Color, source.Dimpher),
				source.Duration);
		}

		public static void DrawCircle(this Meta<CxOrigin> source)
		{
			const int HALF_PRECISION = 12;
			var quatSource = Quaternion.FromToRotation(source.Source.Orientation * Vector3.up, source.UpVector);
			var vector = quatSource * Vector3.forward * source.Size;
			var step = Quaternion.AngleAxis(Mathf.Rad2Deg * Mathf.PI / HALF_PRECISION, source.UpVector);
			for(var index = 0; index < HALF_PRECISION * 2; index++)
			{
				var next = step * vector;
				UnityEngine.Debug.DrawLine(
					source.Source.ConvertSpaceOf(vector),
					source.Source.ConvertSpaceOf(next),
					Color.Lerp(Color.black, source.Color, source.Dimpher),
					source.Duration);
				vector = next;
			}
		}

		public static void DrawSector(this Meta<CxSector> source)
		{
			//var angle = Vector3.Angle(
			//	source.Source.StartAtZero.normalized,
			//	source.Source.StopAtZero.normalized);

			var max = 8f;
			var colorSource = Color.Lerp(Color.black, source.Color, source.IsGradient ? source.Dimpher * .5f : source.Dimpher);
			var colorTarget = Color.Lerp(Color.black, source.Color, source.Dimpher);
			for(var index = 0f; index < max; index += 1f)
			{
				UnityEngine.Debug.DrawLine(
					source.Source.Origin + Vector3.Slerp(source.Source.StartAtZero, source.Source.StopAtZero, index / max),
					source.Source.Origin + Vector3.Slerp(source.Source.StartAtZero, source.Source.StopAtZero, (index + 1f) / max),
					Color.Lerp(colorSource, colorTarget, (index + 1f) / max),
					source.Duration);
			}
		}

		public static void DrawKnotGizmos(this Meta<CxOrigin> source)
		{
			using(new WithColorGizmo(source.Color))
			{
				var p0 = source.Source.Orientation * new Vector3(1f, 1f) * source.Size + source.Source.Location;
				var p1 = source.Source.Orientation * new Vector3(1f, -1f) * source.Size + source.Source.Location;
				var p2 = source.Source.Orientation * new Vector3(-1f, -1f) * source.Size + source.Source.Location;
				var p3 = source.Source.Orientation * new Vector3(-1f, 1f) * source.Size + source.Source.Location;

				Gizmos.DrawLine(p0, p1);
				Gizmos.DrawLine(p1, p2);
				Gizmos.DrawLine(p2, p3);
				Gizmos.DrawLine(p3, p0);
			}
		}

		public static void DrawKnotHandles(this Meta<CxOrigin> source)
		{
			#if UNITY_EDITOR
			using(new WithColorHandle(source.Color))
			{
				var p0 = source.Source.Orientation * new Vector3(1f, 1f) * source.Size + source.Source.Location;
				var p1 = source.Source.Orientation * new Vector3(1f, -1f) * source.Size + source.Source.Location;
				var p2 = source.Source.Orientation * new Vector3(-1f, -1f) * source.Size + source.Source.Location;
				var p3 = source.Source.Orientation * new Vector3(-1f, 1f) * source.Size + source.Source.Location;

				Handles.DrawLine(p0, p1);
				Handles.DrawLine(p1, p2);
				Handles.DrawLine(p2, p3);
				Handles.DrawLine(p3, p0);
			}
			#endif
		}

		public static void Draw(this Meta<CxOrigin> source)
		{
			if(source.ShapeType == Meta<CxOrigin>.Shape.Cross)
			{
				source.DrawCross();
			}
			else if(source.ShapeType == Meta<CxOrigin>.Shape.Circle)
			{
				source.DrawCircle();
			}
			else if(source.ShapeType == Meta<CxOrigin>.Shape.CurveKnot)
			{
				source.DrawKnotHandles();
			}
		}

		//public static void Draw(this Plane source, Color color, float size = DEFAULT_SIZE, float duration = 0f)
		//{
		//	var origin = source.normal * source.distance;
		//	const float STEP = Mathf.PI / 12f;
		//	var unit = Quaternion.FromToRotation(Vector3.up, source.normal) * Vector3.left * size;
		//	for(var delta = 0f; delta < 2f * Mathf.PI; delta += Mathf.PI / 12f)
		//	{
		//		UnityEngine.Debug.DrawLine(
		//			origin + Quaternion.AngleAxis(delta * Mathf.Rad2Deg, source.normal) * unit,
		//			origin + Quaternion.AngleAxis((delta + STEP) * Mathf.Rad2Deg, source.normal) * unit,
		//			color,
		//			duration);
		//	}
		//	UnityEngine.Debug.DrawLine(origin, origin + source.normal * size * 1.2f, Color.cyan, duration);
		//	//DrawCross(origin, Quaternion.LookRotation(source.normal), color, size, duration);
		//}

		public static void Draw(this Meta<CxJoint> source)
		{
			//const float SIZE = .1f;
			//var source = cxJoint.Target.Add(cxJoint.Target.DirectionForward().Mul(-cxJoint.Distance));
			//source.Position.ToUnity().DrawArrow(cxJoint.Target.Position.ToUnity(), color, Color.blue, duration);
			//source.DrawCross(color, duration);
			//source.DrawCircle(cxJoint.Distance * SIZE * .5f, color, duration);
		}

		public static void Draw(this Meta<CxFrustum> source, bool extended = true)
		{
			UnityEngine.Debug.DrawLine(source.Source.VDL, source.Source.VDR, source.Color);
			UnityEngine.Debug.DrawLine(source.Source.VDR, source.Source.VUR, source.Color);
			UnityEngine.Debug.DrawLine(source.Source.VUR, source.Source.VUL, source.Color);
			UnityEngine.Debug.DrawLine(source.Source.VUL, source.Source.VDL, source.Color);

			if(extended)
			{
				source.Source.Down.Draw(Color.green * .5f);
				source.Source.Up.Draw(Color.green);
				source.Source.Left.Draw(Color.red * .5f);
				source.Source.Right.Draw(Color.red);

				source.Source.Screen.Draw(Color.grey);
			}
		}

		[Conditional("DEBUG_FRUSTUM_PLANES")]
		private static void DebugDrawPlanes(this Meta<CxFrustum> cameraFrustum, Color color, float size)
		{
			//for(var index = 0; index < cameraFrustum.Bounds.Length; index++)
			//{
			//	cameraFrustum.Bounds[index].Draw(color * 1.5f, size);
			//}
		}

		[Conditional("DEBUG_FRUSTUM_LINES")]
		private static void DebugDrawRays(this Meta<CxFrustum> cameraFrustum, Color color, float size)
		{
			//UnityEngine.Debug.DrawRay(cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Left].origin, cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Left].direction, color * 1.2f);
			//UnityEngine.Debug.DrawRay(cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Right].origin, cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Right].direction, color * 1.2f);
			//UnityEngine.Debug.DrawRay(cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Top].origin, cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Top].direction, color * 1.2f);
			//UnityEngine.Debug.DrawRay(cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Bottom].origin, cameraFrustum.BoundsProjected[(int)CameraFrustumAdapter.SideType.Bottom].direction, color * 1.2f);
		}

		[Conditional("DEBUG_FRUSTUM_PROJECTION")]
		private static void DebugDrawProjection(this Meta<CxFrustum> cameraFrustum, Color color, float size)
		{
			//cameraFrustum.TargetProjection.Draw(color, size);
			//for(var index = 0; index < cameraFrustum.CornersProjected.Length; index++)
			//{
			//	UnityEngine.Debug.DrawLine(
			//		cameraFrustum.CornersProjected[(index - 1 + cameraFrustum.CornersProjected.Length) % cameraFrustum.CornersProjected.Length],
			//		cameraFrustum.CornersProjected[(index + cameraFrustum.CornersProjected.Length) % cameraFrustum.CornersProjected.Length],
			//		color);
			//}
		}

		public static Vector3 ConvertSpaceOf(this CxOrigin source, Vector3 target)
		{
			return source.Orientation * target + source.Location;
		}

		public static CxFrustum ToFrustum(this Camera source, Plane on)
		{
			var ray1 = source.ViewportPointToRay(new Vector3(1f, 1f));
			if(!on.Raycast(ray1, out var dist1))
			{
				return new CxFrustum();
			}

			var ray2 = source.ViewportPointToRay(new Vector3(1f, 0f));
			if(!on.Raycast(ray2, out var dist2))
			{
				return new CxFrustum();
			}

			var ray3 = source.ViewportPointToRay(new Vector3(0f, 0f));
			if(!on.Raycast(ray3, out var dist3))
			{
				return new CxFrustum();
			}

			var ray4 = source.ViewportPointToRay(new Vector3(0f, 1f));
			if(!on.Raycast(ray4, out var dist4))
			{
				return new CxFrustum();
			}

			var vur = ray1.GetPoint(dist1);
			var vdr = ray2.GetPoint(dist2);
			var vdl = ray3.GetPoint(dist3);
			var vul = ray4.GetPoint(dist4);

			var camera = source.transform;
			var forward = camera.forward;

			return new CxFrustum
			{
				Source = camera.position,
				Forward = forward,

				VUR = vur,
				VDR = vdr,
				VDL = vdl,
				VUL = vul,

				// TODO: assure towards camera
				Screen = on,

				Left = new Plane(vdl, vul, vul + forward),
				Right = new Plane(vur, vdr, vur + forward),
				Up = new Plane(vul, vur, vur + forward),
				Down = new Plane(vdr, vdl, vdr + forward),
			};
		}

		public static CxFrustum Inflate(this CxFrustum source, float offset)
		{
			Vector3 Offset(Vector3 current, Vector3 previous, Vector3 next)
			{
				var planePrev = new Plane(previous, current, current + source.Screen.normal);
				var planeNext = new Plane(current, next, next + source.Screen.normal);
				var dirPrev = (current - previous).normalized;
				var dirNext = (current - next).normalized;
				var compNext = dirPrev * (offset / Vector3.Dot(planeNext.normal, dirPrev));
				var compPrev = dirNext * (offset / Vector3.Dot(planePrev.normal, dirNext));
				return current + compPrev + compNext;
			}

			var vur = Offset(source.VUR, source.VUL, source.VDR);
			var vdr = Offset(source.VDR, source.VUR, source.VDL);
			var vdl = Offset(source.VDL, source.VDR, source.VUL);
			var vul = Offset(source.VUL, source.VDL, source.VUR);

			return new CxFrustum
			{
				Source = source.Source,
				Forward = source.Forward,

				VUR = vur,
				VDR = vdr,
				VDL = vdl,
				VUL = vul,

				Screen = source.Screen,

				Left = new Plane(vdl, vul, vul + source.Forward),
				Right = new Plane(vur, vdr, vur + source.Forward),
				Up = new Plane(vul, vur, vur + source.Forward),
				Down = new Plane(vdr, vdl, vdr + source.Forward),
			};
		}

		// from red rift specialization

		// TODO: down below draw as meta<>

		private const float SCALE_GLOBAL_F = .03f;

		public static void DrawVectorAt(this Vector2 vector, Vector2 position, float scale = 1f)
		{
			using(new WithColorGizmo(Color.blue))
			{
				Gizmos.DrawLine(position, position + vector.normalized * (scale * SCALE_GLOBAL_F));
			}
		}

		public static void DrawAsPoint(this Vector3 position, float scale = 1f)
		{
			DrawAsPoint((Vector2)position, Quaternion.identity, scale);
		}

		public static void DrawAsPoint(this Vector3 position, Quaternion orient, float scale = 1f)
		{
			DrawAsPoint((Vector2)position, orient, scale);
		}

		public static void DrawAsPoint(this Vector2 position, float scale = 1f)
		{
			DrawAsPoint(position, Quaternion.identity, scale);
		}

		public static void DrawAsPoint(this Vector2 position, Quaternion orient, float scale = 1f, float duration = .5f)
		{
			if(Application.isPlaying)
			{
				scale *= SCALE_GLOBAL_F;
				var up = (Vector2)(orient * Vector3.up);
				var down = (Vector2)(orient * Vector3.down);
				var left = (Vector2)(orient * Vector3.left);
				var right = (Vector2)(orient * Vector3.right);
				UnityEngine.Debug.DrawLine(position, position + down * scale, Color.black, duration);
				UnityEngine.Debug.DrawLine(position, position + left * scale, Color.black, duration);
				UnityEngine.Debug.DrawLine(position, position + right * scale, Color.red, duration);
				UnityEngine.Debug.DrawLine(position, position + up * scale, Color.green, duration);
			}
			else
			{
				using(new WithColorGizmo(Color.black))
				{
					scale *= SCALE_GLOBAL_F;
					var up = (Vector2)(orient * Vector3.up);
					var down = (Vector2)(orient * Vector3.down);
					var left = (Vector2)(orient * Vector3.left);
					var right = (Vector2)(orient * Vector3.right);
					Gizmos.DrawLine(position, position + down * scale);
					Gizmos.DrawLine(position, position + left * scale);
					Gizmos.color = Color.red;
					Gizmos.DrawLine(position, position + right * scale);
					Gizmos.color = Color.green;
					Gizmos.DrawLine(position, position + up * scale);
				}
			}
		}
	}

	public struct WithColorGizmo : IDisposable
	{
		private readonly Color _backup;
		private readonly bool _hadChanged;

		public WithColorGizmo(Color color)
		{
			_backup = Gizmos.color;
			Gizmos.color = color;
			_hadChanged = true;
		}

		public void Dispose()
		{
			if(_hadChanged)
			{
				Gizmos.color = _backup;
			}
		}
	}

	#if UNITY_EDITOR
	public struct WithColorHandle : IDisposable
	{
		private readonly Color _backup;
		private readonly bool _hadChanged;

		public WithColorHandle(Color color)
		{
			_backup = Handles.color;
			Handles.color = color;
			_hadChanged = true;
		}

		public void Dispose()
		{
			if(_hadChanged)
			{
				Handles.color = _backup;
			}
		}
	}
	#endif
}
