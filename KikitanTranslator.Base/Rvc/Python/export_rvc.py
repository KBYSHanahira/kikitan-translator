"""Converts an RVC voice model (.pth) into the ONNX file Kikitan runs.

Follows RVC 2.2.231006 infer/modules/onnx/export.py. Kikitan runs it with its own Python in the app folder
(see RvcPythonExport.cs): python -I export_rvc.py <model.pth> <output.onnx>
Exit code 0 on success; 2 with a message on stderr when the file cannot be converted.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import torch  # noqa: E402

from infer_pack.models_onnx import SynthesizerTrnMsNSFsidM  # noqa: E402


def fail(message):
    print(message, file=sys.stderr)
    sys.exit(2)


def main(source, target):
    # A .pth file is a pickle; weights_only refuses anything but tensors and plain data, so loading cannot run code.
    try:
        cpt = torch.load(source, map_location="cpu", weights_only=True)
    except Exception as e:
        fail(f"Unable to read the model: {e}")

    if not isinstance(cpt, dict) or "weight" not in cpt or "config" not in cpt:
        fail("This .pth is not an RVC voice model. Training checkpoints (G_*.pth, D_*.pth) do not work; "
             "use the small model file from RVC's weights folder.")
    if cpt.get("f0", 1) != 1:
        fail("RVC models without pitch guidance are not supported.")
    vocoder = cpt.get("vocoder", "HiFi-GAN")
    if vocoder != "HiFi-GAN":
        fail(f"Models with the {vocoder} vocoder are not supported, only HiFi-GAN.")

    version = cpt.get("version", "v1")
    cpt["config"][-3] = cpt["weight"]["emb_g.weight"].shape[0]
    vec_channels = 256 if version == "v1" else 768

    test_phone = torch.rand(1, 200, vec_channels)
    test_phone_lengths = torch.tensor([200]).long()
    test_pitch = torch.randint(size=(1, 200), low=5, high=255)
    test_pitchf = torch.rand(1, 200)
    test_ds = torch.LongTensor([0])
    test_rnd = torch.rand(1, 192, 200)

    net_g = SynthesizerTrnMsNSFsidM(*cpt["config"], is_half=False, version=version)
    net_g.load_state_dict(cpt["weight"], strict=False)
    net_g.eval()

    partial = target + ".part"
    with torch.no_grad():
        torch.onnx.export(
            net_g,
            (test_phone, test_phone_lengths, test_pitch, test_pitchf, test_ds, test_rnd),
            partial,
            dynamic_axes={"phone": [1], "pitch": [1], "pitchf": [1], "rnd": [2]},
            do_constant_folding=False,
            opset_version=13,
            verbose=False,
            input_names=["phone", "phone_lengths", "pitch", "pitchf", "ds", "rnd"],
            output_names=["audio"],
            dynamo=False,
        )
    os.replace(partial, target)

    print(f"Exported {version} model ({cpt['config'][-1]} Hz)")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        fail("Usage: export_rvc.py <model.pth> <output.onnx>")

    main(sys.argv[1], sys.argv[2])
