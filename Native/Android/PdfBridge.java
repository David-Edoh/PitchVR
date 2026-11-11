package com.pitchvr;

import android.app.Activity;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.pdf.PdfRenderer;
import android.net.Uri;
import android.os.Bundle;
import android.os.ParcelFileDescriptor;
import android.util.Base64;
import java.io.ByteArrayOutputStream;
import java.lang.reflect.Method;

/** Android's system PDF picker and PdfRenderer bridge for the Unity screen. */
public final class PdfBridge {
    private PdfBridge() { }

    public static void openPicker(Activity activity, String unityObject) {
        Intent intent = new Intent(activity, PdfPickerActivity.class);
        intent.putExtra("unityObject", unityObject);
        activity.runOnUiThread(() -> activity.startActivity(intent));
    }

    public static int pageCount(Activity activity, String uriString) throws Exception {
        try (ParcelFileDescriptor descriptor = activity.getContentResolver()
                .openFileDescriptor(Uri.parse(uriString), "r");
             PdfRenderer renderer = new PdfRenderer(descriptor)) {
            return renderer.getPageCount();
        }
    }

    public static String renderPage(Activity activity, String uriString, int index, int width) throws Exception {
        try (ParcelFileDescriptor descriptor = activity.getContentResolver()
                .openFileDescriptor(Uri.parse(uriString), "r");
             PdfRenderer renderer = new PdfRenderer(descriptor);
             PdfRenderer.Page page = renderer.openPage(index)) {
            int height = Math.max(1, Math.round(width * (float) page.getHeight() / page.getWidth()));
            Bitmap image = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
            try {
                image.eraseColor(Color.WHITE);
                page.render(image, null, null, PdfRenderer.Page.RENDER_MODE_FOR_DISPLAY);
                ByteArrayOutputStream output = new ByteArrayOutputStream();
                image.compress(Bitmap.CompressFormat.PNG, 90, output);
                return Base64.encodeToString(output.toByteArray(), Base64.NO_WRAP);
            } finally {
                image.recycle();
            }
        }
    }

    public static class PdfPickerActivity extends Activity {
        private static final int PICK_PDF = 41;
        private String unityObject;

        @Override protected void onCreate(Bundle state) {
            super.onCreate(state);
            unityObject = getIntent().getStringExtra("unityObject");
            Intent picker = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            picker.addCategory(Intent.CATEGORY_OPENABLE);
            picker.setType("application/pdf");
            picker.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION |
                    Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);
            startActivityForResult(picker, PICK_PDF);
        }

        @Override protected void onActivityResult(int request, int result, Intent data) {
            super.onActivityResult(request, result, data);
            if (request == PICK_PDF) {
                String uri = "";
                if (result == RESULT_OK && data != null && data.getData() != null) {
                    Uri picked = data.getData();
                    uri = picked.toString();
                    try {
                        getContentResolver().takePersistableUriPermission(picked,
                                Intent.FLAG_GRANT_READ_URI_PERMISSION);
                    } catch (SecurityException ignored) { }
                }
                try {
                    Class<?> player = Class.forName("com.unity3d.player.UnityPlayer");
                    Method send = player.getMethod("UnitySendMessage", String.class, String.class, String.class);
                    send.invoke(null, unityObject, "OnPdfChosen", uri);
                } catch (Exception ignored) { }
                finish();
            }
        }
    }
}
