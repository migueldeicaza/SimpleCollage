//
// Simple tool to create a collage out of a list of files
//
// Miguel de Icaza

using System;
using System.Linq;
using System.IO;
using SkiaSharp;

class Collage {
	int columns = 8;
	int cellsize = 128;
	string output = "collage.png";
	string directory = ".";

	public Collage (string [] args)
	{
		bool showHelp = false;
		var options = new Mono.Options.OptionSet () {
			{ "cols=", (int cols) => columns = cols },
			{ "cellsize=", (int cell) => cellsize = cell },
			{ "output=", (string file) => output = file },
			{ "h|?|help", v => showHelp = true },

		};
		void Help ()
		{
			Console.WriteLine ("collage [options] DIRECTORY\n" +
					   $"Default output is {output}, columns {columns}, cell size {cellsize}");
			options.WriteOptionDescriptions (Console.Error);
			Environment.Exit (0);
		}
		if (showHelp)
			Help ();

		var dir = options.Parse (args).FirstOrDefault ();
		if (dir == null)
			Help ();

		if (dir != null)
			directory = dir;
	}

	void Run ()
	{
		var images = (from x in new DirectoryInfo (directory).GetFiles ()
			      where x.Name.EndsWith (".jpg") || x.Name.EndsWith (".png")
			      let image = SKBitmap.Decode (Path.Combine (directory, x.Name))
			      let info = image.Info
			      where info.Width > 32 && info.Height > 32
			      orderby x.CreationTimeUtc
			      select image).ToList ();

		var rows = (images.Count + columns + 1) / columns;
		var target = new SKBitmap (cellsize * columns, cellsize * rows, true);
		var canvas = new SKCanvas (target);
		int col = 0, row = 0;

		Console.WriteLine ($"Creating collage with {images.Count ()} images in {output}");
		foreach (var image in images) {
			var location = new SKPoint (col * cellsize, row * cellsize);
			int hsize, xd, yd, wsize;
			if (image.Info.Width > image.Info.Height) {
				hsize = cellsize * image.Info.Height / image.Info.Width;
				yd = (cellsize - hsize) / 2;
				xd = 0;
				wsize = cellsize;
			} else {
				yd = 0;
				hsize = cellsize;
				wsize = cellsize * image.Info.Width / image.Info.Height;
				xd = (cellsize - wsize) / 2;
			}
			var destRect = SKRect.Create (col * cellsize + xd, row * cellsize + yd, wsize, hsize);
			canvas.DrawBitmap (image, destRect);
			col++;
			if (col == columns)
				row++;
		}
		using (var outputStream = File.Create (output)) {
			var kind = SKEncodedImageFormat.Png;
			if (output.EndsWith (".jpg"))
				kind = SKEncodedImageFormat.Jpeg;

			SKImage.FromBitmap (target).Encode (kind, 10).SaveTo (outputStream);
		}
	}

	public static void Main (string [] args)
	{
		try {
			new Collage (args).Run ();
		} catch (DirectoryNotFoundException dir) {
			Console.Error.WriteLine (dir.Message);
		}
	}
}